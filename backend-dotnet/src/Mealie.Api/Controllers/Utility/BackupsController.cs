using Mealie.Infrastructure.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Utility;

[ApiController]
[Route("api/backups")]
[Authorize]
public class BackupsPublicController(IBackupService backupService) : ControllerBase
{
    [HttpGet("available")]
    public async Task<IActionResult> ListBackups()
    {
        var backups = await backupService.ListBackupsAsync();
        return Ok(backups);
    }
}
