using Mealie.Application.Queries;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record BulkDeleteRecipesCommand(IList<string> Slugs) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipes = await db.Recipes.Where(r => Slugs.Contains(r.Slug)).ToListAsync(ct);
        var recipeIds = recipes.Select(r => r.Id).ToList();
        db.Recipes.RemoveRange(recipes);
        await db.SaveChangesAsync(ct);
        if (recipeIds.Count > 0)
        {
            await services.Mediator.Publish(new RecipesBulkDeletedEvent(recipeIds), ct);
        }

        return true;
    }
}
