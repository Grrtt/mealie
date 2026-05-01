using Mealie.Application.Contracts.Search;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Search;

/// <summary>
///     Rebuilds both recipe and food search indexes once on startup to ensure they are up-to-date.
/// </summary>
public class SearchIndexRebuildService(
    IRecipeSearchIndex recipeIndex,
    IFoodSearchIndex foodIndex,
    ILogger<SearchIndexRebuildService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Rebuilding search indexes on startup...");
            await Task.WhenAll(
                recipeIndex.RebuildAsync(stoppingToken),
                foodIndex.RebuildAsync(stoppingToken));
            logger.LogInformation("Search indexes ready");
        }
        catch (OperationCanceledException)
        {
            // Shutting down before rebuild completed — that's fine
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rebuild search indexes on startup");
        }
    }
}
