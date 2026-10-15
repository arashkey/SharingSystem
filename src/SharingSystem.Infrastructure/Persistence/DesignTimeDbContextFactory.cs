using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SharingSystem.Infrastructure.Persistence;

/// <summary>
/// Used only by the EF Core tools (dotnet ef) to create migrations.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DesignTimeConnectionString = "Data Source=design-time.db";

    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(DesignTimeConnectionString).Options;
        return new AppDbContext(options);
    }
}
