using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/backups")]
[Authorize(Roles = "admin")]
public class BackupsController : ControllerBase
{
    [HttpGet]
    public IActionResult ListBackups()
        => Ok(new { imports = Array.Empty<object>(), templates = Array.Empty<object>() });

    [HttpPost]
    public IActionResult CreateBackup()
        => Ok(new { detail = "Backup functionality is not implemented in the .NET backend" });

    [HttpGet("{fileName}")]
    public IActionResult GetBackup(string fileName)
        => NotFound(new { detail = "Backup not found" });

    [HttpDelete("{fileName}")]
    public IActionResult DeleteBackup(string fileName)
        => Ok(new { detail = "Backup deleted" });

    [HttpPost("restore")]
    public IActionResult RestoreBackup([FromBody] object request)
        => StatusCode(501, new { detail = "Restore functionality is not implemented" });
}
