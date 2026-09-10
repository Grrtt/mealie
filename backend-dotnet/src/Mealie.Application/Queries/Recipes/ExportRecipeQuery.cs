using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record ExportRecipeQuery(string Slug) : IQuery<(Recipe Recipe, string FileName)?>
{
    public async Task<(Recipe Recipe, string FileName)?> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var recipe = await services.Db.Recipes
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Nutrition)
            .Include(r => r.Settings)
            .FirstOrDefaultAsync(r => r.Slug == Slug, ct);

        return recipe is null ? null : (recipe, $"{recipe.Slug}.zip");
    }
}
