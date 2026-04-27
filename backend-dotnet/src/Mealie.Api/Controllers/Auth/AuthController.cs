using Mealie.Application.Dtos.Auth;
using Mealie.Application.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    public async Task<ActionResult<TokenResponse>> Login([FromForm] LoginRequest request)
    {
        var result = await authService.LoginAsync(request.Username, request.Password);
        if (result is null)
            return Unauthorized(new { detail = "Incorrect username or password" });
        return Ok(result);
    }

    [HttpPost("token/refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshAsync(request.Token);
        if (result is null)
            return Unauthorized(new { detail = "Invalid or expired refresh token" });
        return Ok(result);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("mealie.access_token");
        return Ok(new { message = "Logged out" });
    }

    [HttpGet("oauth")]
    public IActionResult OAuthRedirect() => Redirect("/api/auth/oauth/login");

    [HttpGet("oauth/callback")]
    public async Task<ActionResult<TokenResponse>> OAuthCallback([FromQuery] string code, [FromQuery] string state)
    {
        await Task.CompletedTask;
        return BadRequest(new { detail = "OIDC not configured" });
    }
}
