using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipeAssetsController(IRecipeAssetService assetService) : ControllerBase
{
    [HttpGet("{slug}/assets")]
    public async Task<ActionResult<IList<AssetResponse>>> GetAssets(string slug, CancellationToken ct)
    {
        var assets = await assetService.GetAssetsAsync(slug, ct);
        return Ok(assets);
    }

    [HttpPost("{slug}/assets")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AssetResponse>> UploadAsset(
        string slug,
        IFormFile file,
        [FromForm] string? name,
        [FromForm] string? icon,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { detail = "No file provided" });

        var ext = Path.GetExtension(file.FileName).TrimStart('.');
        var assetName = name ?? Path.GetFileNameWithoutExtension(file.FileName);
        var assetIcon = icon ?? "mdi-file";

        await using var stream = file.OpenReadStream();
        var asset = await assetService.UploadAssetAsync(slug, stream, assetName, ext, assetIcon, ct);
        if (asset is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(asset);
    }

    [HttpDelete("{slug}/assets/{fileName}")]
    public async Task<IActionResult> DeleteAsset(string slug, string fileName, CancellationToken ct)
    {
        var deleted = await assetService.DeleteAssetAsync(slug, fileName, ct);
        if (!deleted) return NotFound(new { detail = "Asset not found" });
        return NoContent();
    }
}
