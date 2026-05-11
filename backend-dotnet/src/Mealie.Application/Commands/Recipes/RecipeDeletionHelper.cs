using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

internal static class RecipeDeletionHelper
{
    public static async Task DeleteRecipesAsync(ApplicationDbContext db, IReadOnlyCollection<Guid> recipeIds,
        CancellationToken ct = default)
    {
        if (recipeIds.Count == 0)
        {
            return;
        }

        var ids = recipeIds.Distinct().ToArray();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.MealPlans
            .Where(plan => plan.RecipeId.HasValue && ids.Contains(plan.RecipeId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(plan => plan.RecipeId, (Guid?)null), ct);

        await db.ShoppingListItemRecipeReferences.Where(reference => ids.Contains(reference.RecipeId)).ExecuteDeleteAsync(ct);
        await db.ShoppingListRecipeReferences.Where(reference => ids.Contains(reference.RecipeId)).ExecuteDeleteAsync(ct);
        await db.RecipeComments.Where(comment => ids.Contains(comment.RecipeId)).ExecuteDeleteAsync(ct);
        await db.RecipeAssets.Where(asset => ids.Contains(asset.RecipeId)).ExecuteDeleteAsync(ct);
        await db.RecipeShareTokens.Where(token => ids.Contains(token.RecipeId)).ExecuteDeleteAsync(ct);
        await db.RecipeTimelineEvents.Where(entry => ids.Contains(entry.RecipeId)).ExecuteDeleteAsync(ct);
        await db.RecipeNotes.Where(note => ids.Contains(note.RecipeId)).ExecuteDeleteAsync(ct);
        await db.RecipeInstructions.Where(instruction => ids.Contains(instruction.RecipeId)).ExecuteDeleteAsync(ct);
        await db.RecipeIngredients.Where(ingredient => ids.Contains(ingredient.RecipeId)).ExecuteDeleteAsync(ct);

        foreach (var recipeId in ids)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM recipes_to_tags WHERE recipe_id = {recipeId}", ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM recipes_to_categories WHERE recipe_id = {recipeId}", ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM recipes_to_tools WHERE recipe_id = {recipeId}", ct);
        }

        await db.Recipes.IgnoreQueryFilters()
            .Where(recipe => ids.Contains(recipe.Id))
            .ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
    }
}
