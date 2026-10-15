using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Options;
using SharingSystem.Application.Shares;

namespace SharingSystem.Application.Maintenance;

public interface ICleanupService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Removes expired shares, abandoned uploads, expired reverse shares and expired password reset tokens.
/// </summary>
public sealed class CleanupService(
    IAppDbContext dbContext,
    IFileStorage fileStorage,
    IShareRemover shareRemover,
    IOptions<CleanupOptions> options,
    TimeProvider timeProvider,
    ILogger<CleanupService> logger) : ICleanupService
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var incompleteMaxAge = TimeSpan.FromHours(options.Value.IncompleteShareMaxAgeHours);
        var incompleteThreshold = now - incompleteMaxAge;

        var sharesToDelete = await dbContext.Shares
            .Where(s => s.ExpiresAt <= now || (!s.IsCompleted && s.CreatedAt <= incompleteThreshold))
            .ToListAsync(cancellationToken);
        await shareRemover.RemoveAsync(sharesToDelete, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var expiredReverseShares = await dbContext.ReverseShares
            .Where(r => r.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var expiredTokens = await dbContext.PasswordResetTokens
            .Where(t => t.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var stalePartialFiles = await fileStorage.DeleteStalePartialFilesAsync(incompleteMaxAge, cancellationToken);

        logger.LogInformation(
            "Cleanup removed {Shares} shares, {ReverseShares} reverse shares, {Tokens} reset tokens and {Partials} partial uploads",
            sharesToDelete.Count, expiredReverseShares, expiredTokens, stalePartialFiles);
    }
}
