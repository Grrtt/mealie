using Mealie.Application.Queries;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record DeleteRecipeCommand(Guid GroupId, string Slug) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.GroupId == GroupId && r.Slug == Slug, ct);
        if (recipe is null)
        {
            return false;
        }

        var recipeId = recipe.Id;
        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new RecipeDeletedEvent(recipeId, recipe.HouseholdId), ct);
        return true;
    }
}
