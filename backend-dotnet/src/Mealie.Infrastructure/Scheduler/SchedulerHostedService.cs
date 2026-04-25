using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mealie.Infrastructure.Scheduler.Jobs;

namespace Mealie.Infrastructure.Scheduler;

public class SchedulerHostedService(IServiceProvider services, ILogger<SchedulerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scheduler started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDueJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduler tick failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        logger.LogInformation("Scheduler stopped");
    }

    private async Task RunDueJobsAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Run meal plan notifications at 8 AM UTC
        if (now.Hour == 8 && now.Minute < 1)
        {
            using var scope = services.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<MealPlanNotificationJob>();
            await job.RunAsync(ct);
        }

        // Run backup job at 2 AM UTC daily
        if (now.Hour == 2 && now.Minute < 1)
        {
            using var scope = services.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<ScheduledBackupJob>();
            await job.RunAsync(ct);
        }
    }
}
