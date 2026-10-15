using Mapster;
using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Shares;

internal static class ShareFileQueries
{
    /// <summary>
    /// Maps a share to its details with one page of its files (ordered by name) and the totals of all files.
    /// </summary>
    public static async Task<ShareDetailsDto> GetDetailsAsync(
        this IAppDbContext dbContext, ShareEntity share, int filePage, int pageSize, CancellationToken cancellationToken)
    {
        var files = dbContext.ShareFiles.AsNoTracking().Where(f => f.ShareId == share.Id);
        var details = share.Adapt<ShareDetailsDto>();

        // The identifier is a tie-breaker, so the order (and thereby the pages) stays stable.
        details.Files = await files
            .OrderBy(f => f.Name.ToLower())
            .ThenBy(f => f.Id)
            .ProjectToType<ShareFileDto>()
            .ToPagedResultAsync(filePage, pageSize, cancellationToken);
        details.TotalSize = await files.SumAsync(f => f.Size, cancellationToken);
        return details;
    }
}
