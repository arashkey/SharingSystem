using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharingSystem.Infrastructure.Backup;

namespace SharingSystem.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically runs <see cref="IDatabaseBackupService"/>.
/// </summary>
public sealed class DatabaseBackupHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<BackupOptions> options,
    ILogger<DatabaseBackupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.Value.IntervalHours));

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>().CreateSnapshotAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "The database backup job failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
