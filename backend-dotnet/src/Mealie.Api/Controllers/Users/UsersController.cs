using Mealie.Application.Dtos.Users;
using Mealie.Application.Services.Auth;
using Mealie.Application.Services.Users;
using Mealie.Application.Validators.Auth;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        => Ok(new { allow_registration = settings.Value.AllowSignup });

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!registrationService.AllowSignup)
            return BadRequest(new { detail = "Registration is disabled" });

        var success = await registrationService.RegisterAsync(request);
        if (!success)
            return BadRequest(new { detail = "Registration failed — username or email already taken" });

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
            return BadRequest(new { detail = "Invalid or expired reset token" });
        return Ok(new { detail = "Password reset successful" });
    }

    // ── Authenticated Profile ──────────────────────────────────────────────

    [HttpGet("self")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetSelf()
    {
        var user = await userService.GetProfileAsync(tenantContext.UserId);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpPut("self")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> UpdateSelf([FromBody] UpdateUserRequest request)
    {
        var user = await userService.UpdateProfileAsync(tenantContext.UserId, request);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpPut("self/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var success = await userService.ChangePasswordAsync(tenantContext.UserId, request.CurrentPassword, request.NewPassword);
        if (!success) return BadRequest(new { detail = "Current password is incorrect" });
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

    [HttpDelete("self/api-tokens/{tokenId:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteApiToken(int tokenId)
    {
        var success = await userService.DeleteApiKeyAsync(tenantContext.UserId, tokenId);
        if (!success) return NotFound();
        return Ok(new { detail = "API token deleted" });
    }

    // ── User lookup ────────────────────────────────────────────────────────

    [HttpGet("{userId:guid}")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetUser(Guid userId)
    {
        var user = await userService.GetProfileAsync(userId);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpPut("{userId:guid}/favorites/{slug}")]
    [Authorize]
    public async Task<IActionResult> AddFavorite(Guid userId, string slug)
    {
        await userService.AddFavoriteAsync(userId, slug);
        return Ok();
    }

    [HttpDelete("{userId:guid}/favorites/{slug}")]
    [Authorize]
    public async Task<IActionResult> RemoveFavorite(Guid userId, string slug)
    {
        await userService.RemoveFavoriteAsync(userId, slug);
        return Ok();
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
}
