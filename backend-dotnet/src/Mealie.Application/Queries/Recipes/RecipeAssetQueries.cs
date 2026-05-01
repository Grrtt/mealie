using Mealie.Application.Dtos.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record GetAssetsQuery(string Slug) : IQuery<IList<AssetResponse>>
{
    public async Task<IList<AssetResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null) return [];
        return await db.RecipeAssets
            .Where(a => a.RecipeId == recipe.Id)
            .Select(a => new AssetResponse { Id = a.Id, Name = a.Name, Icon = a.Icon, Extension = a.Extension, RecipeId = a.RecipeId })
            .ToListAsync(ct);
    }
}
