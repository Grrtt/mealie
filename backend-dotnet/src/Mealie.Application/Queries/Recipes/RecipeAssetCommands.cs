using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
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

public record UploadAssetCommand(string Slug, Stream FileStream, string Name, string Extension, string Icon) : IQuery<AssetResponse?>
{
    public async Task<AssetResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null) return null;
        var dataDir = services.Settings.Value.DataDir;
        var assetDir = Path.Combine(dataDir, "recipes", recipe.Id.ToString(), "assets");
        Directory.CreateDirectory(assetDir);
        var safeName = Path.GetFileNameWithoutExtension(Name);
        var safeExt = Extension.TrimStart('.');
        var filePath = Path.Combine(assetDir, $"{safeName}.{safeExt}");
        await using var fs = File.Create(filePath);
        await FileStream.CopyToAsync(fs, ct);
        var asset = new RecipeAsset { Id = Guid.NewGuid(), Name = safeName, Icon = Icon, Extension = safeExt, RecipeId = recipe.Id };
        db.RecipeAssets.Add(asset);
        await db.SaveChangesAsync(ct);
        return new AssetResponse { Id = asset.Id, Name = asset.Name, Icon = asset.Icon, Extension = asset.Extension, RecipeId = asset.RecipeId };
    }
}

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
