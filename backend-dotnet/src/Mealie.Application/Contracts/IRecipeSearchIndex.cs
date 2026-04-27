namespace Mealie.Application.Contracts;

public interface IRecipeSearchIndex
{
    Task IndexRecipeAsync(Guid recipeId, CancellationToken ct = default);
    Task RemoveRecipeAsync(Guid recipeId, CancellationToken ct = default);
    Task<RecipeSearchResult> SearchAsync(RecipeSearchQuery query, CancellationToken ct = default);
    Task RebuildAsync(CancellationToken ct = default);
}
