using Mealie.Application.Dtos.Recipes;

namespace Mealie.Application.Services.Recipes;

public interface IRecipeAssetService
{
    Task<IList<AssetResponse>> GetAssetsAsync(string slug, CancellationToken ct = default);
    Task<AssetResponse?> UploadAssetAsync(string slug, Stream fileStream, string name, string extension, string icon, CancellationToken ct = default);
    Task<bool> DeleteAssetAsync(string slug, string fileName, CancellationToken ct = default);
}
