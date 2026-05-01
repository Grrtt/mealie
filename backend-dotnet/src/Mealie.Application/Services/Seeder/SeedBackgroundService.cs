using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Seeder;

/// <summary>
///     Long-running background service that reads seed jobs from the channel and
///     processes them one at a time. Jobs are idempotent — existing entries are skipped.
/// </summary>
public class SeedBackgroundService(
    SeedQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<SeedBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Seed background service started");

        await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing seed job {Type} for group {GroupId}", job.Type,
                    job.GroupId);
            }
        }
    }

    private async Task ProcessJobAsync(SeedJobRequest job, CancellationToken ct)
    {
        logger.LogInformation("Seeding {Type} (locale: {Locale}) for group {GroupId}", job.Type, job.Locale,
            job.GroupId);

        using var scope = scopeFactory.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ISeederService>();

        await (job.Type switch
        {
            SeedType.Foods => seeder.SeedFoodsAsync(job.GroupId, job.Locale, ct),
            SeedType.Labels => seeder.SeedLabelsAsync(job.GroupId, job.Locale, ct),
            SeedType.Units => seeder.SeedUnitsAsync(job.GroupId, job.Locale, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(job.Type), job.Type, null)
        });

        logger.LogInformation("Seeding {Type} complete for group {GroupId}", job.Type, job.GroupId);
    }
}
