using Mealie.Application.Dtos.Auth;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Auth;

/// <summary>
///     Separated from AuthController so [Authorize] isn't overridden by the parent's [AllowAnonymous].
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthRefreshController(IJwtTokenService jwtTokenService, ITenantContext tenantContext) : ControllerBase
{
    // Frontend calls GET /api/auth/refresh with a valid bearer token to receive a fresh one.
    [HttpGet("refresh")]
    public ActionResult<TokenResponse> Refresh()
    {
        var newToken = jwtTokenService.GenerateAccessToken(
            tenantContext.UserId,
            tenantContext.GroupId,
            tenantContext.HouseholdId,
            tenantContext.IsAdmin);
        return Ok(new TokenResponse { AccessToken = newToken, TokenType = "bearer" });
    }
}
