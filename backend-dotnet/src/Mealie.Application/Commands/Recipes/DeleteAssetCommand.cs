using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record DeleteAssetCommand(string Slug, string FileName) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null) return false;
        var baseName = Path.GetFileNameWithoutExtension(FileName);
        var asset = await db.RecipeAssets.FirstOrDefaultAsync(a => a.RecipeId == recipe.Id && a.Name == baseName, ct);
        if (asset is null) return false;
        var dataDir = services.Settings.Value.DataDir;
        var filePath = Path.Combine(dataDir, "recipes", recipe.Id.ToString(), "assets", FileName);
        if (File.Exists(filePath)) File.Delete(filePath);
        db.RecipeAssets.Remove(asset);
        await db.SaveChangesAsync(ct);
        return true;
    }
}