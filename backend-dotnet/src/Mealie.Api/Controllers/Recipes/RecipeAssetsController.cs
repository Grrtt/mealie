using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Recipes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipeAssetsController(QueryExecutor executor) : ControllerBase
{
    [HttpGet("{slug}/assets")]
    public async Task<ActionResult<IList<AssetResponse>>> GetAssets(string slug, CancellationToken ct)
    {
        var assets = await executor.ExecuteAsync(new GetAssetsQuery(slug), ct);
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
        var asset = await executor.ExecuteAsync(new UploadAssetCommand(slug, stream, assetName, ext, assetIcon), ct);
        if (asset is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(asset);
    }

    [HttpDelete("{slug}/assets/{fileName}")]
    public async Task<IActionResult> DeleteAsset(string slug, string fileName, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteAssetCommand(slug, fileName), ct);
        if (!deleted) return NotFound(new { detail = "Asset not found" });
        return NoContent();
    }
}
