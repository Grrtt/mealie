using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Utility;

[ApiController]
public class MediaController(IOptions<AppSettings> settings, ApplicationDbContext db) : ControllerBase
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
    [AllowAnonymous]
    public async Task<IActionResult> RecipeImage(Guid recipeId, string imageName, CancellationToken ct)
    {
        if (HttpContext.User.Identity?.IsAuthenticated != true)
        {
            var isPublic = await IsRecipePubliclyAccessible(recipeId, ct);
            if (!isPublic)
            {
                return Unauthorized();
            }
        }

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
    [AllowAnonymous]
    public async Task<IActionResult> RecipeTimelineEventImage(Guid recipeId, Guid eventId, string imageName, CancellationToken ct)
    {
        if (HttpContext.User.Identity?.IsAuthenticated != true)
        {
            var isPublic = await IsRecipePubliclyAccessible(recipeId, ct);
            if (!isPublic)
            {
                return Unauthorized();
            }
        }

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

    private async Task<bool> IsRecipePubliclyAccessible(Guid recipeId, CancellationToken ct)
    {
        var result = await db.Recipes.IgnoreQueryFilters()
            .Include(r => r.Household).ThenInclude(h => h!.Preferences)
            .Where(r => r.Id == recipeId)
            .Select(r => new
            {
                IsPublic = r.Settings != null && r.Settings.Public,
                IsPrivateHousehold = r.Household != null && r.Household.Preferences != null && r.Household.Preferences.PrivateHousehold
            })
            .FirstOrDefaultAsync(ct);

        return result is not null && result.IsPublic && !result.IsPrivateHousehold;
    }
}
