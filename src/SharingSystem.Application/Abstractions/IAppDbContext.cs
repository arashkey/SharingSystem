using Microsoft.EntityFrameworkCore;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<UserEntity> Users { get; }

    DbSet<ShareEntity> Shares { get; }

    DbSet<ShareFileEntity> ShareFiles { get; }

    DbSet<ReverseShareEntity> ReverseShares { get; }

    DbSet<PasswordResetTokenEntity> PasswordResetTokens { get; }

    DbSet<AppSettingsEntity> AppSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
