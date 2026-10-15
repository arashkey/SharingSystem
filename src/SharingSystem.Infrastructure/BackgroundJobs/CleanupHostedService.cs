using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharingSystem.Application.Maintenance;
using SharingSystem.Application.Options;

namespace SharingSystem.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically runs <see cref="ICleanupService"/>.
/// </summary>
public sealed class CleanupHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<CleanupOptions> options,
    ILogger<CleanupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.IntervalMinutes));

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ICleanupService>().RunAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "The cleanup job failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
