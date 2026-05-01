using Mealie.Application.Dtos.Users;
using Mealie.Application.Services.Auth;
using Mealie.Application.Services.Users;
using Mealie.Application.Validators.Auth;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace Mealie.Api.Controllers.Users;

[ApiController]
[Route("api/users")]
public class UsersController(
    IRegistrationService registrationService,
    IPasswordResetService passwordResetService,
    IUserService userService,
    ITenantContext tenantContext,
    IOptions<AppSettings> settings) : ControllerBase
{
    // ── Unauthenticated ────────────────────────────────────────────────────

    [HttpGet("registration")]
    [AllowAnonymous]
    public IActionResult GetRegistrationInfo()
    {
        return Ok(new { allow_registration = settings.Value.AllowSignup });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!registrationService.AllowSignup)
        {
            return BadRequest(new { detail = "Registration is disabled" });
        }

        var success = await registrationService.RegisterAsync(request);
        if (!success)
        {
            return BadRequest(new { detail = "Registration failed — username or email already taken" });
        }

        return Ok(new { detail = "Registration successful" });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] PasswordResetRequest request)
    {
        await passwordResetService.GenerateResetTokenAsync(request.Email);
        return Ok(new { detail = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] PasswordResetConfirmRequest request)
    {
        var success = await passwordResetService.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { detail = "Invalid or expired reset token" });
        }

        return Ok(new { detail = "Password reset successful" });
    }

    // ── Authenticated Profile ──────────────────────────────────────────────

    [HttpGet("self")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetSelf()
    {
        var user = await userService.GetProfileAsync(tenantContext.UserId);
        // Return 401 so the frontend re-authenticates rather than looping on 404
        if (user is null)
        {
            return Unauthorized(new { detail = "User not found — please log in again" });
        }

        return Ok(user);
    }

    [HttpPut("self")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateSelf([FromBody] UpdateUserRequest request)
    {
        var user = await userService.UpdateProfileAsync(tenantContext.UserId, request);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("self/password")]
    [Authorize]
    public async Task<IActionResult> ChangePasswordSelf([FromBody] ChangePasswordRequest request)
    {
        var success =
            await userService.ChangePasswordAsync(tenantContext.UserId, request.CurrentPassword, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { detail = "Current password is incorrect" });
        }

        return Ok(new { detail = "Password updated successfully" });
    }

    // Alias: frontend calls PUT /api/users/password (not /api/users/self/password)
    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var success =
            await userService.ChangePasswordAsync(tenantContext.UserId, request.CurrentPassword, request.NewPassword);
        if (!success)
        {
            return BadRequest(new { detail = "Current password is incorrect" });
        }

        return Ok(new { detail = "Password updated successfully" });
    }

    [HttpGet("self/api-tokens")]
    [Authorize]
    public async Task<ActionResult<IList<ApiKeyResponse>>> GetApiTokens()
    {
        var keys = await userService.GetApiKeysAsync(tenantContext.UserId);
        return Ok(keys);
    }

    [HttpPost("self/api-tokens")]
    [Authorize]
    public async Task<ActionResult<ApiKeyResponse>> CreateApiToken([FromBody] CreateApiKeyRequest request)
    {
        var key = await userService.CreateApiKeyAsync(tenantContext.UserId, request.Name);
        return Ok(key);
    }

    [HttpPost("api-tokens")]
    [Authorize]
    public async Task<ActionResult<ApiKeyResponse>> CreateApiTokenAlias([FromBody] CreateApiKeyRequest request)
    {
        var key = await userService.CreateApiKeyAsync(tenantContext.UserId, request.Name);
        return Ok(key);
    }

    [HttpDelete("self/api-tokens/{tokenId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteApiToken(int tokenId)
    {
        var success = await userService.DeleteApiKeyAsync(tenantContext.UserId, tokenId);
        if (!success)
        {
            return NotFound();
        }

        return Ok(new { detail = "API token deleted" });
    }

    [HttpDelete("api-tokens/{tokenId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteApiTokenAlias(int tokenId)
    {
        var success = await userService.DeleteApiKeyAsync(tenantContext.UserId, tokenId);
        if (!success)
        {
            return NotFound();
        }

        return Ok(new { detail = "API token deleted" });
    }

    // ── User lookup ────────────────────────────────────────────────────────

    [HttpGet("{userId:guid}")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetUser(Guid userId)
    {
        var user = await userService.GetProfileAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request)
    {
        var user = await userService.UpdateProfileAsync(userId, request);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPut("{userId:guid}/favorites/{slug}")]
    [Authorize]
    public async Task<IActionResult> AddFavorite(Guid userId, string slug)
    {
        await userService.AddFavoriteAsync(userId, slug);
        return Ok();
    }

    [HttpGet("{userId:guid}/favorites")]
    [Authorize]
    public async Task<ActionResult<IList<string>>> GetFavorites(Guid userId)
    {
        var favorites = await userService.GetFavoritesAsync(userId);
        return Ok(favorites);
    }

    [HttpDelete("{userId:guid}/favorites/{slug}")]
    [Authorize]
    public async Task<IActionResult> RemoveFavorite(Guid userId, string slug)
    {
        await userService.RemoveFavoriteAsync(userId, slug);
        return Ok();
    }

    [HttpGet("self/ratings")]
    [Authorize]
    public async Task<ActionResult<IList<UserRatingResponse>>> GetSelfRatings()
    {
        var ratings = await userService.GetRatingsAsync(tenantContext.UserId);
        return Ok(ratings);
    }

    [HttpGet("{userId:guid}/ratings")]
    [Authorize]
    public async Task<ActionResult<IList<UserRatingResponse>>> GetRatings(Guid userId)
    {
        var ratings = await userService.GetRatingsAsync(userId);
        return Ok(ratings);
    }

    [HttpPost("{userId:guid}/ratings/{slug}")]
    [Authorize]
    public async Task<IActionResult> SetRating(Guid userId, string slug, [FromBody] SetRatingRequest request)
    {
        await userService.SetRatingAsync(userId, slug, request.Rating, request.IsFavorite);
        return Ok();
    }

    // ── Profile Image ──────────────────────────────────────────────────────

    [HttpPost("self/image")]
    [Authorize]
    public Task<IActionResult> UploadSelfImage(IFormFile image)
    {
        return SaveProfileImageAsync(tenantContext.UserId, image);
    }

    [HttpPost("{userId:guid}/image")]
    [Authorize]
    public Task<IActionResult> UploadUserImage(Guid userId, IFormFile image)
    {
        return SaveProfileImageAsync(userId, image);
    }

    private async Task<IActionResult> SaveProfileImageAsync(Guid userId, IFormFile image)
    {
        if (image is null || image.Length == 0)
        {
            return BadRequest(new { detail = "No image provided" });
        }

        var dir = Path.Combine(settings.Value.DataDir, "users", userId.ToString());
        Directory.CreateDirectory(dir);
        var destPath = Path.Combine(dir, "profile.webp");

        using var stream = image.OpenReadStream();
        using var original = SKBitmap.Decode(stream);
        if (original is null)
        {
            return BadRequest(new { detail = "Invalid image format" });
        }

        // Resize to 200×200 for the profile thumbnail
        var size = Math.Min(original.Width, original.Height);
        var resized = original.Width != 200 || original.Height != 200
            ? original.Resize(new SKImageInfo(200, 200),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            : original;

        using var skImage = SKImage.FromBitmap(resized);
        using var data = skImage.Encode(SKEncodedImageFormat.Webp, 90);
        await using var output = System.IO.File.OpenWrite(destPath);
        data.SaveTo(output);

        if (!ReferenceEquals(resized, original))
        {
            resized?.Dispose();
        }

        return Ok(new { detail = "Profile image updated" });
    }
}
