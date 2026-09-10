using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Utility;

[ApiController]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class MediaController(
    IOptions<AppSettings> settings,
    ApplicationDbContext db,
    ITenantContext tenant) : ControllerBase
{
    [HttpGet("/api/media/users/{userId}/{fileName}")]
    public async Task<IActionResult> UserMedia(Guid userId, string fileName, CancellationToken ct)
    {
        if (!tenant.IsAuthenticated)
        {
            return Unauthorized();
        }

        // Profile media is visible to the owner or to any member of the same
        // household, but never across groups.
        var canAccess = await db.Users.AnyAsync(u => u.Id == userId
            && u.GroupId == tenant.GroupId
            && (u.Id == tenant.UserId || u.HouseholdId == tenant.HouseholdId), ct);
        if (!canAccess)
        {
            return NotFound();
        }

        return ServeFile(Path.Combine("users", userId.ToString()), fileName, "image/webp");
    }

    [HttpGet("/api/media/recipes/{recipeId:guid}/images/{imageName}")]
    public async Task<IActionResult> RecipeImage(Guid recipeId, string imageName, CancellationToken ct)
    {
        if (!await CanAccessRecipe(recipeId, allowPublic: true, ct))
        {
            return NotFound();
        }

        return ServeFile(Path.Combine("recipes", recipeId.ToString(), "images"), imageName,
            ImageContentType(imageName));
    }

    [HttpGet("/api/media/recipes/{recipeId:guid}/images/timeline/{eventId:guid}/{imageName}")]
    public async Task<IActionResult> RecipeTimelineEventImage(
        Guid recipeId, Guid eventId, string imageName, CancellationToken ct)
    {
        if (!tenant.IsAuthenticated)
        {
            return Unauthorized();
        }

        if (!await CanAccessRecipe(recipeId, allowPublic: false, ct))
        {
            return NotFound();
        }

        return ServeFile(Path.Combine("recipes", recipeId.ToString(), "images", "timeline", eventId.ToString()),
            imageName, "image/webp");
    }

    [HttpGet("/api/media/recipes/{recipeId:guid}/assets/{assetName}")]
    public async Task<IActionResult> RecipeAsset(Guid recipeId, string assetName, CancellationToken ct)
    {
        if (!tenant.IsAuthenticated)
        {
            return Unauthorized();
        }

        if (!await CanAccessRecipe(recipeId, allowPublic: false, ct))
        {
            return NotFound();
        }

        return ServeFile(Path.Combine("recipes", recipeId.ToString(), "assets"), assetName,
            "application/octet-stream");
    }

    private async Task<bool> CanAccessRecipe(Guid recipeId, bool allowPublic, CancellationToken ct)
    {
        if (await OwnedByCaller(recipeId, ct))
        {
            return true;
        }

        return allowPublic && await PubliclyVisible(recipeId, ct);
    }

    private Task<bool> OwnedByCaller(Guid recipeId, CancellationToken ct)
    {
        if (!tenant.IsAuthenticated)
        {
            return Task.FromResult(false);
        }

        // Ignore the tenant filter — ownership is decided explicitly here so
        // callers from other households get a clean NotFound.
        return db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Id == recipeId
            && r.GroupId == tenant.GroupId && r.HouseholdId == tenant.HouseholdId, ct);
    }

    private Task<bool> PubliclyVisible(Guid recipeId, CancellationToken ct)
    {
        // Matches ExploreController: public recipes in non-private households are
        // visible to anyone, including anonymous callers and other households.
        return db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Id == recipeId
            && r.Settings != null && r.Settings.Public
            && (r.Household == null || r.Household.Preferences == null
                || !r.Household.Preferences.PrivateHousehold), ct);
    }

    private IActionResult ServeFile(string relativeDirectory, string fileName, string contentType)
    {
        if (!IsSafeFileName(fileName))
        {
            return BadRequest();
        }

        var directory = Path.GetFullPath(Path.Combine(settings.Value.DataDir, relativeDirectory));
        var filePath = Path.Combine(directory, fileName);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        return PhysicalFile(filePath, contentType);
    }

    private static bool IsSafeFileName(string fileName)
    {
        // The name must be a single plain file name: no empty or dot names, no
        // slashes of either kind, no NUL bytes, and no rooted paths — nothing
        // that could escape the media directory.
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
        {
            return false;
        }

        return fileName.IndexOfAny(['/', '\\', '\0']) < 0
               && !Path.IsPathRooted(fileName)
               && Path.GetFileName(fileName) == fileName;
    }

    private static string ImageContentType(string imageName)
    {
        return Path.GetExtension(imageName).ToLowerInvariant() switch
        {
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
