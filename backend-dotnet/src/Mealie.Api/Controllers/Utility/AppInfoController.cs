using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Utility;

[ApiController]
[AllowAnonymous]
public class AppInfoController : ControllerBase
{
    [HttpGet("/api/app/about")]
    public IActionResult About() => Ok(new
    {
        production = true,
        version = "2.0.0",
        versionLatest = "2.0.0",
        demoStatus = false,
        allowSignup = false,
        isFirstLogin = false,
        enableOidc = false,
        oidcRedirect = (string?)null,
        oidcProviderName = (string?)null,
    });

    [HttpGet("/api/app/about/oidc")]
    public IActionResult AboutOidc() => Ok(new { enabled = false });

    [HttpGet("/api/debug/version")]
    public IActionResult DebugVersion() => Ok(new { version = "2.0.0" });
}
