using Mealie.Infrastructure.Admin;
using Microsoft.Extensions.Logging;

namespace Mealie.Infrastructure.Scheduler.Jobs;

public class ScheduledBackupJob(IBackupService backupService, ILogger<ScheduledBackupJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Running scheduled backup job");
        try
        {
            var result = await backupService.CreateBackupAsync(ct);
            logger.LogInformation("Backup created: {FileName}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled backup failed");
        }
    }
}
