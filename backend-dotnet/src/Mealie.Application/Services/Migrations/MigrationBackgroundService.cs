using Mealie.Application.Services.Recipes;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Scraper.Importers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Migrations;

/// <summary>
/// Long-running background service that reads migration jobs from the channel
/// and processes them asynchronously, writing results to the DB.
/// Multiple uploads queue up and are processed one at a time in order.
/// </summary>
public class MigrationBackgroundService(
    MigrationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<MigrationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Migration background service started");

        await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing migration job {ReportId}", job.ReportId);
            }
        }
    }

    private async Task ProcessJobAsync(MigrationJobRequest job, CancellationToken ct)
    {
        logger.LogInformation("Processing migration job {ReportId} (file: {Path})", job.ReportId, job.TempFilePath);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var parsers = scope.ServiceProvider.GetRequiredService<IEnumerable<IMigrationParser>>();
        var recipeService = scope.ServiceProvider.GetRequiredService<IRecipeService>();

        var report = await db.Reports.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == job.ReportId, ct);

        if (report is null)
        {
            logger.LogWarning("Report {ReportId} not found, skipping", job.ReportId);
            TryDeleteTempFile(job.TempFilePath);
            return;
        }

        try
        {
            await using var stream = File.OpenRead(job.TempFilePath);

            var parser = parsers.FirstOrDefault(p => p.CanParse(stream));
            if (parser is null)
            {
                await FailReportAsync(db, report, "Unsupported import format", ct);
                return;
            }

            stream.Position = 0;

            var entries = new List<ReportEntry>();
            int created = 0, skipped = 0, errors = 0;

            foreach (var scraped in parser.Parse(stream))
            {
                try
                {
                    if (string.IsNullOrEmpty(scraped.Name))
                    {
                        skipped++;
                        entries.Add(Entry(job.ReportId, false, "Skipped: recipe has no name"));
                        continue;
                    }

                    await recipeService.CreateFromScrapedAsync(scraped, job.HouseholdId, job.GroupId, ct);
                    created++;
                    entries.Add(Entry(job.ReportId, true, $"Imported: {scraped.Name}"));
                }
                catch (Exception ex)
                {
                    errors++;
                    logger.LogWarning(ex, "Failed to import recipe {Name}", scraped.Name);
                    entries.Add(Entry(job.ReportId, false, $"Failed: {scraped.Name}", ex.Message));
                }
            }

            await db.ReportEntries.AddRangeAsync(entries, ct);
            report.Status = errors == 0 ? "success" : (created > 0 ? "partial" : "failure");
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Migration {ReportId} complete — created: {Created}, skipped: {Skipped}, errors: {Errors}",
                job.ReportId, created, skipped, errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration job {ReportId} failed unexpectedly", job.ReportId);
            await FailReportAsync(db, report, "Unexpected error during import", ct, ex.Message);
        }
        finally
        {
            TryDeleteTempFile(job.TempFilePath);
        }
    }

    private static async Task FailReportAsync(
        ApplicationDbContext db,
        Report report,
        string message,
        CancellationToken ct,
        string? exception = null)
    {
        report.Status = "failure";
        await db.ReportEntries.AddAsync(Entry(report.Id, false, message, exception), ct);
        await db.SaveChangesAsync(ct);
    }

    private static ReportEntry Entry(Guid reportId, bool success, string message, string? exception = null) =>
        new() { Id = Guid.NewGuid(), ReportId = reportId, Timestamp = DateTime.UtcNow, Success = success, Message = message, Exception = exception };

    private void TryDeleteTempFile(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not delete temp file {Path}", path); }
    }
}
