using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Recipes;

public class RecipeAssetService(ApplicationDbContext db, IOptions<AppSettings> settings) : IRecipeAssetService
{
    private readonly string _dataDir = settings.Value.DataDir;

    public async Task<IList<AssetResponse>> GetAssetsAsync(string slug, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (recipe is null) return [];

        return await db.RecipeAssets
            .Where(a => a.RecipeId == recipe.Id)
            .Select(a => new AssetResponse
            {
                Id = a.Id,
                Name = a.Name,
                Icon = a.Icon,
                Extension = a.Extension,
                RecipeId = a.RecipeId,
            })
            .ToListAsync(ct);
    }

    public async Task<AssetResponse?> UploadAssetAsync(
        string slug, Stream fileStream, string name, string extension, string icon, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (recipe is null) return null;

        var assetDir = Path.Combine(_dataDir, "recipes", recipe.Id.ToString(), "assets");
        Directory.CreateDirectory(assetDir);

        var safeName = Path.GetFileNameWithoutExtension(name);
        var safeExt = extension.TrimStart('.');
        var filePath = Path.Combine(assetDir, $"{safeName}.{safeExt}");

        await using var fs = File.Create(filePath);
        await fileStream.CopyToAsync(fs, ct);

        var asset = new RecipeAsset
        {
            Id = Guid.NewGuid(),
            Name = safeName,
            Icon = icon,
            Extension = safeExt,
            RecipeId = recipe.Id,
        };

        db.RecipeAssets.Add(asset);
        await db.SaveChangesAsync(ct);

        return new AssetResponse
        {
            Id = asset.Id,
            Name = asset.Name,
            Icon = asset.Icon,
            Extension = asset.Extension,
            RecipeId = asset.RecipeId,
        };
    }

    public async Task<bool> DeleteAssetAsync(string slug, string fileName, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (recipe is null) return false;

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var asset = await db.RecipeAssets
            .FirstOrDefaultAsync(a => a.RecipeId == recipe.Id && a.Name == baseName, ct);
        if (asset is null) return false;

        var filePath = Path.Combine(_dataDir, "recipes", recipe.Id.ToString(), "assets", fileName);
        if (File.Exists(filePath)) File.Delete(filePath);

        db.RecipeAssets.Remove(asset);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
