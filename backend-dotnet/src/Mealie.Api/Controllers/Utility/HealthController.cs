using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Utility;

[ApiController]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet("/healthz")]
    public IActionResult Healthz() => Ok(new { status = "ok", version = "2.0.0" });

    [HttpGet("/readyz")]
    public IActionResult Readyz() => Ok(new { status = "ok", version = "2.0.0", database = "connected" });
}
