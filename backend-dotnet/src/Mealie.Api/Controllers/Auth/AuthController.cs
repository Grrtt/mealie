using Mealie.Application.Dtos.Auth;
using Mealie.Application.Services.Auth;
using Mealie.Infrastructure.Auth;
using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(
    IAuthService authService,
    IOidcService oidcService,
    IJwtTokenService jwtTokenService,
    IOptions<AppSettings> settings) : ControllerBase
{
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    public async Task<ActionResult<TokenResponse>> Login([FromForm] LoginRequest request)
    {
        var result = await authService.LoginAsync(request.Username, request.Password);
        if (result is null)
        {
            return Unauthorized(new { detail = "Incorrect username or password" });
        }

        return Ok(result);
    }

    [HttpPost("token/refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshAsync(request.Token);
        if (result is null)
        {
            return Unauthorized(new { detail = "Invalid or expired refresh token" });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("mealie.access_token");
        return Ok(new { message = "Logged out" });
    }

    [HttpGet("oauth/providers")]
    public IActionResult GetOAuthProviders()
    {
        var providers = new List<object>();
        if (oidcService.IsConfigured)
        {
            providers.Add(new
            {
                name = "oidc",
                displayName = "SSO",
                redirectUrl = $"{settings.Value.BaseUrl}/api/auth/oauth"
            });
        }

        return Ok(providers);
    }

    [HttpGet("oauth")]
    public IActionResult OAuthRedirect()
    {
        if (!oidcService.IsConfigured)
        {
            return BadRequest(new { detail = "OIDC not configured" });
        }

        var redirectUri = $"{settings.Value.BaseUrl}/api/auth/oauth/callback";
        var state = Guid.NewGuid().ToString("N");
        var nonce = Guid.NewGuid().ToString("N");

        var authUrl = oidcService.GetAuthorizationUrl(redirectUri, state, nonce);
        if (authUrl is null)
        {
            return BadRequest(new { detail = "Could not build OIDC authorization URL" });
        }

        return Redirect(authUrl);
    }

    [HttpGet("oauth/callback")]
    public async Task<ActionResult<TokenResponse>> OAuthCallback([FromQuery] string code, [FromQuery] string? state)
    {
        if (!oidcService.IsConfigured)
        {
            return BadRequest(new { detail = "OIDC not configured" });
        }

        var redirectUri = $"{settings.Value.BaseUrl}/api/auth/oauth/callback";
        var userInfo = await oidcService.ExchangeCodeAsync(code, redirectUri, HttpContext.RequestAborted);
        if (userInfo is null)
        {
            return Unauthorized(new { detail = "OIDC code exchange failed" });
        }

        var user = await oidcService.ProvisionUserAsync(userInfo, HttpContext.RequestAborted);
        if (user is null)
        {
            return Unauthorized(new { detail = "User provisioning failed" });
        }

        var token = jwtTokenService.GenerateAccessToken(
            user.Id, user.GroupId, user.HouseholdId ?? Guid.Empty, user.Admin);

        return Ok(new TokenResponse { AccessToken = token, TokenType = "bearer" });
    }
}
