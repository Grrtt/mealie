using Mealie.Infrastructure.Scraper;
using Mealie.Infrastructure.Scraper.Importers;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Recipes;

public class MigrationImportService(
    IEnumerable<IMigrationParser> parsers,
    IRecipeService recipeService,
    ILogger<MigrationImportService> logger)
{
    public async Task<MigrationImportReport> ImportAsync(Guid groupId, Guid householdId, Guid userId, Stream inputStream, CancellationToken ct = default)
    {
        var parser = parsers.FirstOrDefault(p => p.CanParse(inputStream));
        if (parser is null)
        {
            return new MigrationImportReport { Error = "Unsupported import format. Supported: Mealie JSON, Chowdown, Paprika, Nextcloud Cookbook, Tandoor" };
        }

        var report = new MigrationImportReport();
        foreach (var scraped in parser.Parse(inputStream))
        {
            try
            {
                if (string.IsNullOrEmpty(scraped.Name))
                {
                    report.Skipped++;
                    continue;
                }

                await recipeService.CreateFromScrapedAsync(scraped, householdId, groupId, ct);
                report.Created++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to import recipe: {Name}", scraped.Name);
                report.Errors++;
            }
        }

        return report;
    }
}

public class MigrationImportReport
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public int Errors { get; set; }
    public string? Error { get; set; }
}
