using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Notifications;
using SharingSystem.Application.Options;
using SharingSystem.Application.Settings;
using SharingSystem.Domain.Constants;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Shares;

public interface IShareUploadService
{
    Task<bool> IsIdAvailableAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Returns the upload limits for a normal upload or for an upload through a reverse share.</summary>
    Task<UploadLimitsDto> GetLimitsAsync(string? reverseShareToken, CancellationToken cancellationToken = default);

    Task<CreatedShareDto> CreateAsync(CreateShareRequest request, CancellationToken cancellationToken = default);

    Task<UploadChunkResult> UploadChunkAsync(UploadChunkRequest request, CancellationToken cancellationToken = default);

    Task<CompletedShareDto> CompleteAsync(string shareId, string? uploadToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implements the upload flow: create a draft share, upload files in chunks, complete the share.
/// </summary>
public sealed partial class ShareUploadService(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    IPasswordHasher passwordHasher,
    IContentTypeResolver contentTypeResolver,
    ISettingsService settingsService,
    INotificationService notificationService,
    IAppUrlResolver appUrlResolver,
    IOptions<UploadOptions> uploadOptions,
    TimeProvider timeProvider) : IShareUploadService
{
    private const int MaxIdGenerationAttempts = 10;
    private const string AnonymousCreator = "Someone";

    public async Task<bool> IsIdAvailableAsync(string id, CancellationToken cancellationToken = default) =>
        IsValidShareId(id) && !await dbContext.Shares.AnyAsync(s => s.Id == id, cancellationToken);

    public async Task<UploadLimitsDto> GetLimitsAsync(string? reverseShareToken, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        var maxSize = settings.MaxShareSizeBytes;

        if (!string.IsNullOrEmpty(reverseShareToken))
        {
            var reverseShare = await GetUsableReverseShareAsync(reverseShareToken, cancellationToken);
            maxSize = reverseShare.MaxShareSizeBytes;
        }

        return new UploadLimitsDto(maxSize, GetEffectiveChunkSize(settings), settings.MaxExpirationHours);
    }

    public async Task<CreatedShareDto> CreateAsync(CreateShareRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        ReverseShareEntity? reverseShare = null;
        if (!string.IsNullOrEmpty(request.ReverseShareToken))
        {
            reverseShare = await GetUsableReverseShareAsync(request.ReverseShareToken, cancellationToken);
        }
        else if (!currentUser.IsAuthenticated && !settings.AllowUnauthenticatedShares)
        {
            throw AppException.Forbidden("You need to sign in to create a share.");
        }

        var simplified = reverseShare?.IsSimplified == true;
        var id = simplified || string.IsNullOrWhiteSpace(request.Id)
            ? await GenerateUniqueIdAsync(cancellationToken)
            : await ValidateCustomIdAsync(request.Id.Trim(), cancellationToken);

        // Simplified reverse shares hide all options, so the longest allowed lifetime is used.
        var expiresAt = simplified
            ? settings.MaxExpirationHours == 0 ? (DateTime?)null : now.AddHours(settings.MaxExpirationHours)
            : ExpirationCalculator.Calculate(request.Expiration, now, settings.MaxExpirationHours);

        List<string> recipients = simplified ? [] : NormalizeRecipients(request.Recipients, settings);
        var uploadToken = SecureTokens.CreateUrlSafeToken();

        var share = new ShareEntity
        {
            Id = id,
            Name = simplified ? null : TextNormalizer.NormalizeOptional(request.Name, DomainLimits.ShareNameMaxLength),
            Description = simplified ? null : TextNormalizer.NormalizeOptional(request.Description, DomainLimits.ShareDescriptionMaxLength),
            PasswordHash = simplified || string.IsNullOrEmpty(request.Password) ? null : passwordHasher.Hash(request.Password),
            MaxViews = simplified ? null : ValidateMaxViews(request.MaxViews),
            ExpiresAt = expiresAt,
            UploadTokenHash = SecureTokens.Hash(uploadToken),
            RecipientEmails = recipients.Count == 0 ? null : string.Join(',', recipients),
            CreatorId = reverseShare?.CreatorId ?? currentUser.UserId,
            ReverseShareId = reverseShare?.Id,
            IsCreatorOnly = reverseShare is { IsPublicAccess: false },
            CreatedAt = now
        };

        dbContext.Shares.Add(share);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CreatedShareDto(share.Id, uploadToken);
    }

    public async Task<UploadChunkResult> UploadChunkAsync(UploadChunkRequest request, CancellationToken cancellationToken = default)
    {
        var share = await dbContext.Shares
            .Include(s => s.ReverseShare)
            .FirstOrDefaultAsync(s => s.Id == request.ShareId, cancellationToken)
            ?? throw AppException.NotFound("The share does not exist.");

        EnsureCanUpload(share, request.UploadToken);

        if (request.TotalChunks < 1 || request.ChunkIndex < 0 || request.ChunkIndex >= request.TotalChunks)
        {
            throw AppException.Validation("The chunk index is invalid.");
        }

        var settings = await settingsService.GetAsync(cancellationToken);
        var chunkSize = GetEffectiveChunkSize(settings);
        var isFirstChunk = request.ChunkIndex == 0;
        var isLastChunk = request.ChunkIndex == request.TotalChunks - 1;

        var fileId = request.FileId ?? (isFirstChunk
            ? Guid.NewGuid()
            : throw AppException.Validation("The file identifier is missing."));

        var completedFileShareId = await dbContext.ShareFiles
            .Where(f => f.Id == fileId)
            .Select(f => f.ShareId)
            .FirstOrDefaultAsync(cancellationToken);
        if (completedFileShareId is not null)
        {
            // The last chunk was stored, but its response got lost and the client retried it.
            if (isLastChunk && completedFileShareId == share.Id)
            {
                return new UploadChunkResult(fileId, true);
            }

            throw AppException.Validation("This file was already uploaded.");
        }

        var partialLength = await fileStorage.GetPartialLengthAsync(share.Id, fileId, cancellationToken);
        var expectedOffset = (long)request.ChunkIndex * chunkSize;
        if (!isLastChunk && partialLength == expectedOffset + chunkSize)
        {
            // The chunk was stored, but its response got lost and the client retried it.
            return new UploadChunkResult(fileId, false);
        }

        if (partialLength != expectedOffset)
        {
            throw AppException.Validation("The chunk was received out of order. Please restart the upload.");
        }

        var usedBytes = await dbContext.ShareFiles
            .Where(f => f.ShareId == share.Id)
            .SumAsync(f => f.Size, cancellationToken);
        var maxShareSize = share.ReverseShare?.MaxShareSizeBytes ?? settings.MaxShareSizeBytes;
        var remainingBytes = Math.Max(0, maxShareSize - usedBytes - partialLength);
        var chunkLimit = Math.Min(chunkSize, remainingBytes);

        if (!await fileStorage.HasFreeSpaceForAsync(chunkLimit, cancellationToken))
        {
            await fileStorage.DeletePartialFileAsync(share.Id, fileId, cancellationToken);
            throw AppException.InsufficientStorage();
        }

        var result = await fileStorage.AppendChunkAsync(share.Id, fileId, request.Content, chunkLimit, cancellationToken);
        if (result.LimitExceeded)
        {
            await fileStorage.DeletePartialFileAsync(share.Id, fileId, cancellationToken);
            throw remainingBytes < chunkSize
                ? AppException.Validation("The share exceeds the maximum size of {0}.", ByteSize.Format(maxShareSize))
                : AppException.Validation("The chunk is larger than the configured chunk size.");
        }

        if (!isLastChunk && result.BytesWritten != chunkSize)
        {
            await fileStorage.DeletePartialFileAsync(share.Id, fileId, cancellationToken);
            throw AppException.Validation("The chunk size does not match the server configuration. Please reload the page.");
        }

        if (!isLastChunk)
        {
            return new UploadChunkResult(fileId, false);
        }

        var size = await fileStorage.CompleteFileAsync(share.Id, fileId, cancellationToken);
        var fileName = FileNameSanitizer.Sanitize(request.FileName);

        dbContext.ShareFiles.Add(new ShareFileEntity
        {
            Id = fileId,
            ShareId = share.Id,
            Name = fileName,
            Size = size,
            ContentType = contentTypeResolver.Resolve(fileName),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteArchiveAsync(share.Id, cancellationToken);

        return new UploadChunkResult(fileId, true);
    }

    public async Task<CompletedShareDto> CompleteAsync(string shareId, string? uploadToken, CancellationToken cancellationToken = default)
    {
        var share = await dbContext.Shares
            .Include(s => s.Creator)
            .Include(s => s.ReverseShare)
            .ThenInclude(r => r!.Creator)
            .FirstOrDefaultAsync(s => s.Id == shareId, cancellationToken)
            ?? throw AppException.NotFound("The share does not exist.");

        if (share.IsCompleted || !SecureTokens.Matches(uploadToken, share.UploadTokenHash))
        {
            throw AppException.Forbidden();
        }

        if (!await dbContext.ShareFiles.AnyAsync(f => f.ShareId == share.Id, cancellationToken))
        {
            throw AppException.Validation("Add at least one file to the share.");
        }

        if (share.ReverseShare is not null)
        {
            var updated = await dbContext.ReverseShares
                .Where(r => r.Id == share.ReverseShare.Id && r.RemainingUses > 0)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RemainingUses, r => r.RemainingUses - 1), cancellationToken);

            if (updated == 0)
            {
                throw AppException.Validation("This reverse share link has no remaining uses.");
            }
        }

        share.IsCompleted = true;
        share.UploadTokenHash = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken);
        var url = $"{baseUrl}/s/{share.Id}";

        if (!string.IsNullOrEmpty(share.RecipientEmails))
        {
            await notificationService.SendShareToRecipientsAsync(
                share.RecipientEmails.Split(','), url, share.Creator?.Username ?? AnonymousCreator,
                share.Description, share.ExpiresAt, cancellationToken);
        }

        if (share.ReverseShare is { SendEmailNotification: true, Creator: not null })
        {
            await notificationService.SendReverseShareUsedAsync(share.ReverseShare.Creator.Email, url, cancellationToken);
        }

        return new CompletedShareDto(share.Id, share.IsCreatorOnly ? null : url, !share.IsCreatorOnly);
    }

    /// <summary>The chunk size can never exceed the request body limit of the host.</summary>
    private int GetEffectiveChunkSize(AppSettingsDto settings) =>
        (int)Math.Min(settings.ChunkSizeBytes, uploadOptions.Value.MaxRequestBodyBytes);

    private void EnsureCanUpload(ShareEntity share, string? uploadToken)
    {
        // Draft shares can be filled by whoever holds the upload token.
        if (!share.IsCompleted && SecureTokens.Matches(uploadToken, share.UploadTokenHash))
        {
            return;
        }

        // Completed shares can be extended by their owner or an administrator.
        if (share.IsCompleted && currentUser.CanManage(share.CreatorId))
        {
            return;
        }

        throw AppException.Forbidden();
    }

    private async Task<ReverseShareEntity> GetUsableReverseShareAsync(string token, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var reverseShare = await dbContext.ReverseShares.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

        if (reverseShare is null || reverseShare.ExpiresAt <= now || reverseShare.RemainingUses <= 0)
        {
            throw AppException.NotFound("This reverse share link is invalid or expired.");
        }

        return reverseShare;
    }

    private async Task<string> GenerateUniqueIdAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxIdGenerationAttempts; attempt++)
        {
            var id = SecureTokens.CreateAlphaNumeric(DomainLimits.GeneratedShareIdLength);
            if (!await dbContext.Shares.AnyAsync(s => s.Id == id, cancellationToken))
            {
                return id;
            }
        }

        throw new InvalidOperationException("Could not generate a unique share identifier.");
    }

    private async Task<string> ValidateCustomIdAsync(string id, CancellationToken cancellationToken)
    {
        if (!IsValidShareId(id))
        {
            throw AppException.Validation(
                "The link must contain {0} to {1} letters, digits, dashes or underscores.",
                DomainLimits.ShareIdMinLength,
                DomainLimits.ShareIdMaxLength);
        }

        if (await dbContext.Shares.AnyAsync(s => s.Id == id, cancellationToken))
        {
            throw AppException.Validation("This link is already in use.");
        }

        return id;
    }

    private static bool IsValidShareId(string? id) =>
        !string.IsNullOrEmpty(id)
        && id.Length >= DomainLimits.ShareIdMinLength
        && id.Length <= DomainLimits.ShareIdMaxLength
        && ShareIdRegex().IsMatch(id);

    private static int? ValidateMaxViews(int? maxViews)
    {
        if (maxViews is < 1)
        {
            throw AppException.Validation("The maximum number of views must be at least 1.");
        }

        return maxViews;
    }

    private static List<string> NormalizeRecipients(IEnumerable<string> recipients, AppSettingsDto settings)
    {
        var normalized = recipients
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
        {
            return normalized;
        }

        if (!settings.EnableShareEmailRecipients || !settings.CanSendEmails)
        {
            throw AppException.Validation("Sending shares to e-mail recipients is disabled.");
        }

        if (normalized.Count > DomainLimits.MaxRecipients)
        {
            throw AppException.Validation("A share can be sent to at most {0} recipients.", DomainLimits.MaxRecipients);
        }

        var invalid = normalized.FirstOrDefault(r => !MailAddress.TryCreate(r, out _));
        if (invalid is not null)
        {
            throw AppException.Validation("The e-mail address \"{0}\" is invalid.", invalid);
        }

        return normalized;
    }


    [GeneratedRegex(DomainLimits.ShareIdPattern)]
    private static partial Regex ShareIdRegex();
}
