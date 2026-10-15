using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<ShareEntity> Shares => Set<ShareEntity>();

    public DbSet<ShareFileEntity> ShareFiles => Set<ShareFileEntity>();

    public DbSet<ReverseShareEntity> ReverseShares => Set<ReverseShareEntity>();

    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();

    public DbSet<AppSettingsEntity> AppSettings => Set<AppSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All dates are stored and read as UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
