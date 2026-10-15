using Mapster;
using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Settings;
using SharingSystem.Application.Shares;
using SharingSystem.Domain.Constants;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.ReverseShares;

public sealed class ReverseShareDto
{
    public Guid Id { get; init; }
    public string Token { get; init; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public long MaxShareSizeBytes { get; init; }
    public int RemainingUses { get; init; }
    public bool SendEmailNotification { get; init; }
    public bool IsSimplified { get; init; }
    public bool IsPublicAccess { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<ShareSummaryDto> Shares { get; init; } = [];
}

public sealed class CreateReverseShareRequest
{
    public ExpirationRequest Expiration { get; init; } = new();
    public long MaxShareSizeBytes { get; init; }
    public int MaxUseCount { get; init; } = 1;
    public bool SendEmailNotification { get; init; }
    public bool IsSimplified { get; init; }
    public bool IsPublicAccess { get; init; } = true;
}

/// <summary>Information that the reverse share upload page needs.</summary>
public sealed record ReverseShareUploadDto(string Token, long MaxShareSizeBytes, bool IsSimplified, string CreatorUsername);

public interface IReverseShareService
{
    Task<IReadOnlyList<ReverseShareDto>> GetMineAsync(CancellationToken cancellationToken = default);

    Task<ReverseShareDto> CreateAsync(CreateReverseShareRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReverseShareUploadDto> GetForUploadAsync(string token, CancellationToken cancellationToken = default);
}

public sealed class ReverseShareService(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    ISettingsService settingsService,
    IAppUrlResolver appUrlResolver,
    TimeProvider timeProvider) : IReverseShareService
{
    private const int MaxUseCountLimit = 1000;

    public async Task<IReadOnlyList<ReverseShareDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var reverseShares = await dbContext.ReverseShares.AsNoTracking()
            .Where(r => r.CreatorId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ProjectToType<ReverseShareDto>()
            .ToListAsync(cancellationToken);

        var baseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken);
        foreach (var reverseShare in reverseShares)
        {
            reverseShare.Url = BuildUrl(baseUrl, reverseShare.Token);
        }

        return reverseShares;
    }

    public async Task<ReverseShareDto> CreateAsync(CreateReverseShareRequest request, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        var settings = await settingsService.GetAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (request.Expiration.NeverExpires)
        {
            throw AppException.Validation("A reverse share must expire.");
        }

        if (request.MaxShareSizeBytes < 1 || request.MaxShareSizeBytes > settings.MaxShareSizeBytes)
        {
            throw AppException.Validation(
                "The maximum share size must be between 1 byte and {0}.", ByteSize.Format(settings.MaxShareSizeBytes));
        }

        if (request.MaxUseCount is < 1 or > MaxUseCountLimit)
        {
            throw AppException.Validation("The maximum use count must be between 1 and {0}.", MaxUseCountLimit);
        }

        if (request.SendEmailNotification && !settings.CanSendEmails)
        {
            throw AppException.Validation("E-mail notifications are not available because SMTP is not configured.");
        }

        var reverseShare = new ReverseShareEntity
        {
            Id = Guid.NewGuid(),
            Token = SecureTokens.CreateAlphaNumeric(DomainLimits.ReverseShareTokenLength),
            CreatorId = userId,
            ExpiresAt = ExpirationCalculator.Calculate(request.Expiration, now, settings.MaxExpirationHours)!.Value,
            MaxShareSizeBytes = request.MaxShareSizeBytes,
            RemainingUses = request.MaxUseCount,
            SendEmailNotification = request.SendEmailNotification,
            IsSimplified = request.IsSimplified,
            IsPublicAccess = request.IsPublicAccess,
            CreatedAt = now
        };

        dbContext.ReverseShares.Add(reverseShare);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = reverseShare.Adapt<ReverseShareDto>();
        dto.Url = BuildUrl(await appUrlResolver.GetBaseUrlAsync(cancellationToken), reverseShare.Token);
        return dto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reverseShare = await dbContext.ReverseShares.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                           ?? throw AppException.NotFound("The reverse share does not exist.");

        if (!currentUser.CanManage(reverseShare.CreatorId))
        {
            throw AppException.Forbidden();
        }

        dbContext.ReverseShares.Remove(reverseShare);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReverseShareUploadDto> GetForUploadAsync(string token, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var reverseShare = await dbContext.ReverseShares.AsNoTracking()
            .Include(r => r.Creator)
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

        if (reverseShare is null || reverseShare.ExpiresAt <= now || reverseShare.RemainingUses <= 0)
        {
            throw AppException.NotFound("This reverse share link is invalid or expired.");
        }

        return new ReverseShareUploadDto(
            reverseShare.Token,
            reverseShare.MaxShareSizeBytes,
            reverseShare.IsSimplified,
            reverseShare.Creator?.Username ?? string.Empty);
    }

    private static string BuildUrl(string baseUrl, string token) => $"{baseUrl}/upload/{token}";
}
