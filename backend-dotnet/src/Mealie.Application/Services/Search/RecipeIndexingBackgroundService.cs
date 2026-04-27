using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Mealie.Application.Services.Search;

public enum RecipeIndexEventType { Upsert, Delete }
public record RecipeIndexEvent(RecipeIndexEventType Type, Guid RecipeId);

/// <summary>
/// Background service that rebuilds the recipe search index on startup and
/// processes incremental index updates from the <see cref="Channel{RecipeIndexEvent}"/>.
/// </summary>
public class RecipeIndexingBackgroundService(
    RecipeSearchIndex recipeSearchIndex,
    Channel<RecipeIndexEvent> indexChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<RecipeIndexingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Recipe indexing background service started");

        await Task.Run(() => RebuildIndexAsync(stoppingToken), stoppingToken);

        await foreach (var evt in indexChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await Task.Run(() => ProcessEventAsync(evt, stoppingToken), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing recipe index event {Type} for {RecipeId}",
                    evt.Type, evt.RecipeId);
            }
        }
    }

    private async Task RebuildIndexAsync(CancellationToken ct)
    {
        logger.LogInformation("Rebuilding recipe search index from database");
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var recipes = await db.Recipes.IgnoreQueryFilters()
                .Include(r => r.Tags)
                .Include(r => r.Categories)
                .ToListAsync(ct);

            recipeSearchIndex.ClearAllDocuments();

            foreach (var recipe in recipes)
            {
                var summary = new Dtos.Recipes.RecipeSummaryResponse
                {
                    Id = recipe.Id,
                    Name = recipe.Name,
                    Slug = recipe.Slug,
                    Description = recipe.Description,
                    HouseholdId = recipe.HouseholdId,
                    GroupId = recipe.GroupId,
                    Tags = recipe.Tags
                        .Select(t => new Dtos.Recipes.OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug })
                        .ToList(),
                    Categories = recipe.Categories
                        .Select(c => new Dtos.Recipes.OrganizerSimpleResponse { Id = c.Id, Name = c.Name, Slug = c.Slug })
                        .ToList(),
                };
                recipeSearchIndex.IndexRecipe(summary);
            }

            logger.LogInformation("Recipe index rebuild complete — {Count} recipes indexed", recipes.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rebuild recipe search index");
        }
    }

    private async Task ProcessEventAsync(RecipeIndexEvent evt, CancellationToken ct)
    {
        if (evt.Type == RecipeIndexEventType.Delete)
        {
            recipeSearchIndex.RemoveRecipe(evt.RecipeId);
            return;
        }

        // Upsert: re-fetch recipe from DB and reindex
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var recipe = await db.Recipes.IgnoreQueryFilters()
                .Where(r => r.Id == evt.RecipeId)
                .Include(r => r.Tags)
                .Include(r => r.Categories)
                .FirstOrDefaultAsync(ct);

            if (recipe is null)
            {
                logger.LogWarning("Recipe {RecipeId} not found for index upsert, removing from index", evt.RecipeId);
                recipeSearchIndex.RemoveRecipe(evt.RecipeId);
                return;
            }

            var summary = new Dtos.Recipes.RecipeSummaryResponse
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Slug = recipe.Slug,
                Description = recipe.Description,
                HouseholdId = recipe.HouseholdId,
                GroupId = recipe.GroupId,
                Tags = recipe.Tags
                    .Select(t => new Dtos.Recipes.OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug })
                    .ToList(),
                Categories = recipe.Categories
                    .Select(c => new Dtos.Recipes.OrganizerSimpleResponse { Id = c.Id, Name = c.Name, Slug = c.Slug })
                    .ToList(),
            };
            recipeSearchIndex.IndexRecipe(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upsert recipe {RecipeId} in search index", evt.RecipeId);
        }
    }
}
