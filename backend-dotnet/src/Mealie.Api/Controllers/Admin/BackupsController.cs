using Mealie.Infrastructure.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/backups")]
[Authorize(Roles = "admin")]
public class BackupsController(IBackupService backupService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListBackups()
    {
        var backups = await backupService.ListBackupsAsync();
        return Ok(backups);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBackup()
    {
        var fileName = await backupService.CreateBackupAsync();
        return Ok(new { fileName });
    }

    [HttpGet("{fileName}")]
    public IActionResult GetBackup(string fileName)
        => NotFound(new { detail = "Backup not found" });

    [HttpDelete("{fileName}")]
    public async Task<IActionResult> DeleteBackup(string fileName)
    {
        await backupService.DeleteBackupAsync(fileName);
        return Ok(new { detail = "Backup deleted" });
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreBackup([FromBody] RestoreBackupRequest request)
    {
        try
        {
            await backupService.RestoreBackupAsync(request.FileName);
            return Ok(new { detail = "Backup restored successfully" });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { detail = $"Backup not found: {request.FileName}" });
        }
    }
}

public class RestoreBackupRequest
{
    public string FileName { get; set; } = string.Empty;
}
