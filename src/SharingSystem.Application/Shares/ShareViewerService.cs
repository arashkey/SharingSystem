using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Settings;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Shares;

public interface IShareViewerService
{
    /// <summary>Returns a share that the current visitor is allowed to see, with one page of its files.</summary>
    Task<ShareViewDto> GetAsync(string shareId, int filePage, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Validates the password and the view limit and counts a new visit.</summary>
    Task RegisterVisitAsync(string shareId, string? password, CancellationToken cancellationToken = default);

    Task<FileContentDto> OpenFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken = default);

    Task<FileContentDto> OpenArchiveAsync(string shareId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read access to shares for visitors of the public share page.
/// Access tokens (password/visit) are handled by the presentation layer.
/// </summary>
public sealed class ShareViewerService(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    IPasswordHasher passwordHasher,
    ISettingsService settingsService,
    TimeProvider timeProvider) : IShareViewerService
{
    private const string NotFoundMessage = "This share does not exist or has expired.";
    private const string ArchiveExtension = ".zip";
    private const string ZipContentType = "application/zip";

    public async Task<ShareViewDto> GetAsync(
        string shareId, int filePage, int pageSize, CancellationToken cancellationToken = default)
    {
        var share = await LoadVisibleShareAsync(shareId, cancellationToken);
        var details = await dbContext.GetDetailsAsync(share, filePage, pageSize, cancellationToken);

        return new ShareViewDto { Share = details, CanManage = currentUser.CanManage(share.CreatorId) };
    }

    public async Task RegisterVisitAsync(string shareId, string? password, CancellationToken cancellationToken = default)
    {
        var share = await LoadVisibleShareAsync(shareId, cancellationToken);
        if (currentUser.CanManage(share.CreatorId))
        {
            return;
        }

        if (share.PasswordHash is not null && !passwordHasher.Verify(share.PasswordHash, password ?? string.Empty))
        {
            throw AppException.Validation("The password is wrong.");
        }

        var updated = await dbContext.Shares
            .Where(s => s.Id == share.Id && (s.MaxViews == null || s.Views < s.MaxViews))
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Views, s => s.Views + 1), cancellationToken);

        if (updated == 0)
        {
            throw AppException.Validation("This share has reached its maximum number of views.");
        }
    }

    public async Task<FileContentDto> OpenFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken = default)
    {
        // Only the requested file is loaded, so downloads stay fast for shares with many files.
        var share = await LoadVisibleShareAsync(shareId, cancellationToken);
        var file = await dbContext.ShareFiles.AsNoTracking()
                       .FirstOrDefaultAsync(f => f.Id == fileId && f.ShareId == share.Id, cancellationToken)
                   ?? throw AppException.NotFound("The file does not exist.");

        var stream = await fileStorage.OpenReadAsync(share.Id, file.Id, cancellationToken);
        return new FileContentDto(stream, file.Name, file.ContentType);
    }

    public async Task<FileContentDto> OpenArchiveAsync(string shareId, CancellationToken cancellationToken = default)
    {
        var share = await LoadVisibleShareAsync(shareId, cancellationToken);
        var settings = await settingsService.GetAsync(cancellationToken);

        var entries = await dbContext.ShareFiles.AsNoTracking()
            .Where(f => f.ShareId == share.Id)
            .Select(f => new ArchiveEntry(f.Id, f.Name))
            .ToListAsync(cancellationToken);
        var stream = await fileStorage.OpenArchiveAsync(share.Id, entries, settings.ZipCompression, cancellationToken);
        var archiveName = FileNameSanitizer.Sanitize(share.Name ?? share.Id) + ArchiveExtension;
        return new FileContentDto(stream, archiveName, ZipContentType);
    }

    private async Task<ShareEntity> LoadVisibleShareAsync(string shareId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var share = await dbContext.Shares.AsNoTracking()
            .Include(s => s.Creator)
            .FirstOrDefaultAsync(s => s.Id == shareId, cancellationToken);

        if (share is null || !share.IsCompleted || share.ExpiresAt <= now)
        {
            throw AppException.NotFound(NotFoundMessage);
        }

        if (share.IsCreatorOnly && !currentUser.CanManage(share.CreatorId))
        {
            throw AppException.NotFound(NotFoundMessage);
        }

        return share;
    }
}
