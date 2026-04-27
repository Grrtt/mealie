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

    [HttpGet("/api/media/users/{userId}/profile.webp")]
    public IActionResult UserProfileImage(Guid userId)
    {
        var path = Path.Combine(DataDir, "users", userId.ToString(), "profile.webp");
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "image/webp");
    }

    [HttpGet("/api/media/recipes/{slug}/images/{imageName}")]
    public IActionResult RecipeImage(string slug, string imageName)
    {
        var path = Path.Combine(DataDir, "recipes", slug, "images", imageName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        var mimeType = Path.GetExtension(imageName).ToLowerInvariant() switch
        {
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
        return PhysicalFile(path, mimeType);
    }

    [HttpGet("/api/media/recipes/{slug}/assets/{assetName}")]
    public IActionResult RecipeAsset(string slug, string assetName)
    {
        var path = Path.Combine(DataDir, "recipes", slug, "assets", assetName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "application/octet-stream");
    }
}
