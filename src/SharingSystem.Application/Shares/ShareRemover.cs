using SharingSystem.Application.Abstractions;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Shares;

/// <summary>
/// Removes shares together with their stored files.
/// </summary>
public interface IShareRemover
{
    /// <summary>Deletes the stored files and marks the shares as removed. The caller saves the changes.</summary>
    Task RemoveAsync(IEnumerable<ShareEntity> shares, CancellationToken cancellationToken = default);
}

public sealed class ShareRemover(IAppDbContext dbContext, IFileStorage fileStorage) : IShareRemover
{
    public async Task RemoveAsync(IEnumerable<ShareEntity> shares, CancellationToken cancellationToken = default)
    {
        foreach (var share in shares)
        {
            await fileStorage.DeleteShareAsync(share.Id, cancellationToken);
            dbContext.Shares.Remove(share);
        }
    }
}
