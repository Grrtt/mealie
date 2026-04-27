using Mealie.Application.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Search;

/// <summary>
/// Rebuilds the recipe search index once on startup to ensure it is up-to-date.
/// </summary>
public class RecipeIndexRebuildService(
    IRecipeSearchIndex index,
    ILogger<RecipeIndexRebuildService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting recipe search index rebuild on startup");
            await index.RebuildAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down before rebuild completed — that's fine
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rebuild recipe search index on startup");
        }
    }
}
