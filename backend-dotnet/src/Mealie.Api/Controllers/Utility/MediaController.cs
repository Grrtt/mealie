using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Utility;

[ApiController]
[AllowAnonymous]
public class MediaController(IOptions<AppSettings> settings) : ControllerBase
{
    private string DataDir => settings.Value.DataDir;

    [HttpGet("/api/media/users/{userId}/{fileName}")]
    public IActionResult UserMedia(Guid userId, string fileName)
    {
        var userDir = Path.GetFullPath(Path.Combine(DataDir, "users", userId.ToString()));
        var filePath = Path.GetFullPath(Path.Combine(userDir, fileName));

        if (!filePath.StartsWith(userDir, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        return PhysicalFile(filePath, "image/webp");
    }

    [HttpGet("/api/media/recipes/{recipeId:guid}/images/{imageName}")]
    public IActionResult RecipeImage(Guid recipeId, string imageName)
    {
        var imagesDir = Path.GetFullPath(Path.Combine(DataDir, "recipes", recipeId.ToString(), "images"));
        var filePath = Path.GetFullPath(Path.Combine(imagesDir, imageName));

        if (!filePath.StartsWith(imagesDir, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var mimeType = Path.GetExtension(imageName).ToLowerInvariant() switch
        {
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
        return PhysicalFile(filePath, mimeType);
    }

    [HttpGet("/api/media/recipes/{recipeId:guid}/images/timeline/{eventId:guid}/{imageName}")]
    public IActionResult RecipeTimelineEventImage(Guid recipeId, Guid eventId, string imageName)
    {
        var eventDir = Path.GetFullPath(
            Path.Combine(DataDir, "recipes", recipeId.ToString(), "images", "timeline", eventId.ToString()));
        var filePath = Path.GetFullPath(Path.Combine(eventDir, imageName));

        if (!filePath.StartsWith(eventDir, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        return PhysicalFile(filePath, "image/webp");
    }

    [HttpGet("/api/media/recipes/{recipeId:guid}/assets/{assetName}")]
    public IActionResult RecipeAsset(Guid recipeId, string assetName)
    {
        var assetsDir = Path.GetFullPath(Path.Combine(DataDir, "recipes", recipeId.ToString(), "assets"));
        var filePath = Path.GetFullPath(Path.Combine(assetsDir, assetName));

        if (!filePath.StartsWith(assetsDir, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        return PhysicalFile(filePath, "application/octet-stream");
    }
}
