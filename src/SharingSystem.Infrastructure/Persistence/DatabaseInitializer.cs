using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharingSystem.Application.Settings;

namespace SharingSystem.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations and makes sure the settings row exists.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<ISettingsService>().GetAsync(cancellationToken);
    }
}
