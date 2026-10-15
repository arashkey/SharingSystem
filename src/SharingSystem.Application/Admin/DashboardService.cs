using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;

namespace SharingSystem.Application.Admin;

public sealed record DashboardStatsDto(
    int UserCount,
    int ShareCount,
    int FileCount,
    long TotalStorageBytes,
    int TotalViews,
    int ActiveReverseShareCount);

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}

public sealed class DashboardService(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IDashboardService
{
    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAdmin)
        {
            throw AppException.Forbidden();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var completedShares = dbContext.Shares.Where(s => s.IsCompleted);

        return new DashboardStatsDto(
            await dbContext.Users.CountAsync(cancellationToken),
            await completedShares.CountAsync(cancellationToken),
            await dbContext.ShareFiles.CountAsync(cancellationToken),
            await dbContext.ShareFiles.SumAsync(f => f.Size, cancellationToken),
            await completedShares.SumAsync(s => s.Views, cancellationToken),
            await dbContext.ReverseShares.CountAsync(r => r.ExpiresAt > now && r.RemainingUses > 0, cancellationToken));
    }
}
