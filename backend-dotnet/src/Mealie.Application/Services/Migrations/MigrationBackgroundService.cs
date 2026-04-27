using Mealie.Application.Services.ImageScrape;
using Mealie.Application.Services.Images;
using Mealie.Application.Services.IngredientParser;
using Mealie.Application.Services.Recipes;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Scraper.Importers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Migrations;

/// <summary>
/// Long-running background service that reads migration jobs from the channel
/// and processes them asynchronously, writing results to the DB.
/// Multiple uploads queue up and are processed one at a time in order.
/// </summary>
public class MigrationBackgroundService(
    MigrationQueue queue,
    ImageScrapeQueue imageScrapeQueue,
    IServiceScopeFactory scopeFactory,
    IOptions<AppSettings> appSettings,
    ILogger<MigrationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Migration background service started");

        // Re-queue any jobs that were in-progress when the container last stopped.
        await ResumeInterruptedJobsAsync(stoppingToken);

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

    private async Task ResumeInterruptedJobsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var interrupted = await db.Reports.IgnoreQueryFilters()
            .Where(r => r.Category == "migration"
                     && (r.Status == "in-progress" || r.Status == "queued")
                     && r.QueuedFilePath != null
                     && r.QueuedHouseholdId != null)
            .ToListAsync(ct);

        foreach (var report in interrupted)
        {
            if (!File.Exists(report.QueuedFilePath))
            {
                logger.LogWarning("Queued migration file missing for report {ReportId}, marking failed", report.Id);
                report.Status = "failure";
                report.QueuedFilePath = null;
                continue;
            }

            logger.LogInformation("Resuming interrupted migration job {ReportId} ({File})",
                report.Id, Path.GetFileName(report.QueuedFilePath));

            await queue.EnqueueAsync(new MigrationJobRequest(
                report.Id, report.GroupId,
                report.QueuedHouseholdId!.Value,
                report.QueuedUserId ?? report.GroupId,
                report.QueuedFilePath!), ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private const int BatchSize = 50;

    private async Task ProcessJobAsync(MigrationJobRequest job, CancellationToken ct)
    {
        logger.LogInformation("Processing migration job {ReportId} (file: {Path})", job.ReportId, job.TempFilePath);

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var parsers = scope.ServiceProvider.GetRequiredService<IEnumerable<IMigrationParser>>();
        var recipeService = scope.ServiceProvider.GetRequiredService<IRecipeService>();
        // Singleton — resolved from scope but lives for the app lifetime; manages the Python process.
        var ingredientParser = scope.ServiceProvider.GetRequiredService<IngredientParserService>();

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

            // Materialize the full list so we know the total count upfront for the progress meter.
            var allRecipes = parser.Parse(stream).ToList();

            report.Status = "in-progress";
            report.TotalCount = allRecipes.Count;
            await db.SaveChangesAsync(ct);

            // Pre-load foods and units once for the whole job — they don't change mid-migration.
            var foods = await db.Foods.IgnoreQueryFilters()
                .Where(f => f.GroupId == job.GroupId)
                .Include(f => f.Aliases)
                .ToListAsync(ct);

            var units = await db.Units.IgnoreQueryFilters()
                .Where(u => u.GroupId == job.GroupId)
                .ToListAsync(ct);

            var pendingEntries = new List<ReportEntry>(BatchSize);
            int created = 0, skipped = 0, errors = 0;

            // Process BatchSize recipes at a time so we can make a single Python NLP call per chunk
            // instead of one call per recipe.
            foreach (var batch in allRecipes.Chunk(BatchSize))
            {
                // Collect all ingredient strings from all recipes in this chunk.
                var allIngredients = new List<string>();
                var ingredientCounts = new int[batch.Length];
                for (var i = 0; i < batch.Length; i++)
                {
                    ingredientCounts[i] = batch[i].RecipeIngredient.Count;
                    allIngredients.AddRange(batch[i].RecipeIngredient);
                }

                // One Python call for the entire chunk.
                var allParsed = allIngredients.Count > 0
                    ? await ingredientParser.ParseBatchAsync(allIngredients, ct)
                    : (IReadOnlyList<ParsedIngredientResult>)[];

                int offset = 0;
                for (var i = 0; i < batch.Length; i++)
                {
                    var scraped = batch[i];
                    var count = ingredientCounts[i];
                    var recipeIngredients = allParsed.Skip(offset).Take(count).ToList();
                    offset += count;

                    try
                    {
                        if (string.IsNullOrEmpty(scraped.Name))
                        {
                            skipped++;
                            pendingEntries.Add(Entry(job.ReportId, false, "Skipped: recipe has no name"));
                        }
                        else
                        {
                            var result = await recipeService.CreateFromScrapedAsync(
                                scraped, job.HouseholdId, job.GroupId,
                                recipeIngredients, foods, units, ct);

                            if (result is null)
                            {
                                // Already exists — skip on resume.
                                skipped++;
                                pendingEntries.Add(Entry(job.ReportId, true, $"Skipped (already exists): {scraped.Name}"));
                            }
                            else
                            {
                                created++;
                                pendingEntries.Add(Entry(job.ReportId, true, $"Imported: {scraped.Name}"));

                                if (scraped.ImageFiles.Count > 0)
                                    SaveRecipeImages(result.Id.ToString(), scraped.ImageFiles);

                                if (result.OrgUrl is not null && scraped.ImageFiles.Count == 0)
                                    await imageScrapeQueue.Writer.WriteAsync(new ImageScrapeJob(result.Id, result.OrgUrl), ct);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        logger.LogWarning(ex, "Failed to import recipe {Name}", scraped.Name);
                        pendingEntries.Add(Entry(job.ReportId, false, $"Failed: {scraped.Name}", ex.Message));
                    }
                }

                // Flush entries, update progress, and verify the report hasn't been deleted between chunks.
                if (pendingEntries.Count > 0)
                {
                    await db.ReportEntries.AddRangeAsync(pendingEntries, ct);
                    pendingEntries.Clear();
                }

                report.ProcessedCount = created + skipped + errors;
                await db.SaveChangesAsync(ct);

                var stillExists = await db.Reports.IgnoreQueryFilters()
                    .AnyAsync(r => r.Id == job.ReportId, ct);
                if (!stillExists)
                {
                    logger.LogInformation("Report {ReportId} was deleted — stopping migration early", job.ReportId);
                    return;
                }
            }

            report.Status = errors == 0 ? "success" : (created > 0 ? "partial" : "failure");
            report.QueuedFilePath = null;
            report.QueuedHouseholdId = null;
            report.QueuedUserId = null;
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Migration {ReportId} complete — created: {Created}, skipped: {Skipped}, errors: {Errors}",
                job.ReportId, created, skipped, errors);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration job {ReportId} failed unexpectedly", job.ReportId);
            report.QueuedFilePath = null;
            report.QueuedHouseholdId = null;
            report.QueuedUserId = null;
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

    private void SaveRecipeImages(string recipeId, Dictionary<string, byte[]> imageFiles)
    {
        var dir = Path.Combine(appSettings.Value.DataDir, "recipes", recipeId, "images");
        foreach (var (_, data) in imageFiles)
        {
            try
            {
                RecipeImageProcessor.SaveVariants(dir, data);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not save image for recipe {Id}", recipeId);
            }
        }
    }

    private void TryDeleteTempFile(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not delete temp file {Path}", path); }
    }
}
