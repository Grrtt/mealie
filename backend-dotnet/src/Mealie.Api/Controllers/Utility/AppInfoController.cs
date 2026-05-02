using Mealie.Api.Commands;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Utility;

[ApiController]
[AllowAnonymous]
public class AppInfoController(IOptions<AppSettings> settings, ApplicationDbContext db) : ControllerBase
{
    [HttpGet("/api/app/about")]
    public IActionResult About()
    {
        var isFirstLogin = db.Users.Any(u => u.Email == SeedCommand.DefaultEmail);

        return Ok(new
        {
            production = true,
            version = "2.0.0",
            versionLatest = "2.0.0",
            demoStatus = false,
            allowSignup = settings.Value.AllowSignup,
            allowPasswordLogin = true,
            isFirstLogin,
            enableOidc = settings.Value.OidcEnabled,
            oidcRedirect = (string?)null,
            oidcProviderName = (string?)null,
            tokenTime = 48,
            enableOpenai = db.Set<Mealie.Domain.Entities.Settings.AiConfiguration>().Any(),
            enableOpenaiImageServices = false
        });
    }

    [HttpGet("/api/app/about/startup-info")]
    public IActionResult StartupInfo()
    {
        var isFirstLogin = db.Users.Any(u => u.Email == SeedCommand.DefaultEmail);
        return Ok(new { isFirstLogin, isDemo = false });
    }

    [HttpGet("/api/app/about/theme")]
    public IActionResult Theme()
    {
        return Ok(new
        {
            lightPrimary = "#E58325",
            lightAccent = "#007A99",
            lightSecondary = "#973542",
            lightSuccess = "#43A047",
            lightInfo = "#1976D2",
            lightWarning = "#FF6D00",
            lightError = "#EF5350",
            darkPrimary = "#E58325",
            darkAccent = "#007A99",
            darkSecondary = "#973542",
            darkSuccess = "#43A047",
            darkInfo = "#1976D2",
            darkWarning = "#FF6D00",
            darkError = "#EF5350"
        });
    }

    [HttpGet("/api/app/about/oidc")]
    public IActionResult AboutOidc()
    {
        return Ok(new { enabled = settings.Value.OidcEnabled });
    }

    [HttpGet("/api/debug/version")]
    public IActionResult DebugVersion()
    {
        return Ok(new { version = "2.0.0" });
    }
}
