using Mapster;
using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Settings;
using SharingSystem.Domain.Constants;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Shares;

public interface IShareManagementService
{
    Task<IReadOnlyList<ShareSummaryDto>> GetMySharesAsync(CancellationToken cancellationToken = default);

    /// <summary>All completed shares of all users (administrators only).</summary>
    Task<IReadOnlyList<ShareSummaryDto>> GetAllSharesAsync(CancellationToken cancellationToken = default);

    Task<ShareDetailsDto> GetForEditAsync(string shareId, CancellationToken cancellationToken = default);

    Task UpdateAsync(string shareId, UpdateShareRequest request, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string shareId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Management of shares by their owner or by administrators.
/// </summary>
public sealed class ShareManagementService(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    IPasswordHasher passwordHasher,
    IShareRemover shareRemover,
    ISettingsService settingsService,
    TimeProvider timeProvider) : IShareManagementService
{
    public async Task<IReadOnlyList<ShareSummaryDto>> GetMySharesAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.RequireUserId();
        return await dbContext.Shares.AsNoTracking()
            .Where(s => s.CreatorId == userId && s.IsCompleted)
            .OrderByDescending(s => s.CreatedAt)
            .ProjectToType<ShareSummaryDto>()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShareSummaryDto>> GetAllSharesAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        return await dbContext.Shares.AsNoTracking()
            .Where(s => s.IsCompleted)
            .OrderByDescending(s => s.CreatedAt)
            .ProjectToType<ShareSummaryDto>()
            .ToListAsync(cancellationToken);
    }

    public async Task<ShareDetailsDto> GetForEditAsync(string shareId, CancellationToken cancellationToken = default)
    {
        var share = await LoadManageableShareAsync(shareId, includeFiles: true, cancellationToken);
        var details = share.Adapt<ShareDetailsDto>();
        details.Files = [.. details.Files.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)];
        return details;
    }

    public async Task UpdateAsync(string shareId, UpdateShareRequest request, CancellationToken cancellationToken = default)
    {
        var share = await LoadManageableShareAsync(shareId, includeFiles: false, cancellationToken);

        if (request.MaxViews is < 1)
        {
            throw AppException.Validation("The maximum number of views must be at least 1.");
        }

        share.Name = TextNormalizer.NormalizeOptional(request.Name, DomainLimits.ShareNameMaxLength);
        share.Description = TextNormalizer.NormalizeOptional(request.Description, DomainLimits.ShareDescriptionMaxLength);
        share.MaxViews = request.MaxViews;

        if (request.ChangeExpiration)
        {
            var settings = await settingsService.GetAsync(cancellationToken);
            share.ExpiresAt = ExpirationCalculator.Calculate(
                request.Expiration, timeProvider.GetUtcNow().UtcDateTime, settings.MaxExpirationHours);
        }

        if (request.RemovePassword)
        {
            share.PasswordHash = null;
        }
        else if (!string.IsNullOrEmpty(request.NewPassword))
        {
            share.PasswordHash = passwordHasher.Hash(request.NewPassword);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken = default)
    {
        var share = await LoadManageableShareAsync(shareId, includeFiles: true, cancellationToken);
        var file = share.Files.FirstOrDefault(f => f.Id == fileId)
                   ?? throw AppException.NotFound("The file does not exist.");

        if (share.Files.Count == 1)
        {
            throw AppException.Validation("A share needs at least one file. Delete the share instead.");
        }

        await fileStorage.DeleteFileAsync(share.Id, file.Id, cancellationToken);
        await fileStorage.DeleteArchiveAsync(share.Id, cancellationToken);
        dbContext.ShareFiles.Remove(file);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string shareId, CancellationToken cancellationToken = default)
    {
        var share = await LoadManageableShareAsync(shareId, includeFiles: false, cancellationToken);
        await shareRemover.RemoveAsync([share], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ShareEntity> LoadManageableShareAsync(string shareId, bool includeFiles, CancellationToken cancellationToken)
    {
        IQueryable<ShareEntity> query = dbContext.Shares.Include(s => s.Creator);
        if (includeFiles)
        {
            query = query.Include(s => s.Files);
        }

        var share = await query.FirstOrDefaultAsync(s => s.Id == shareId, cancellationToken)
                    ?? throw AppException.NotFound("The share does not exist.");

        if (!currentUser.CanManage(share.CreatorId))
        {
            throw AppException.Forbidden();
        }

        return share;
    }

    private void EnsureAdmin()
    {
        if (!currentUser.IsAdmin)
        {
            throw AppException.Forbidden();
        }
    }

}
