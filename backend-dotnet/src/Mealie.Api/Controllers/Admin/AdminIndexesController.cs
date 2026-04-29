using Mealie.Application.Dtos.Admin;
using Mealie.Application.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/indexes")]
[Authorize(Roles = "admin")]
public class AdminIndexesController(IIndexAdminService indexService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(indexService.GetAll());

    [HttpGet("{name}")]
    public IActionResult Get(string name)
    {
        var info = indexService.GetInfo(name);
        return info is null ? NotFound(new { detail = $"Index '{name}' not found" }) : Ok(info);
    }

    [HttpPost("{name}/rebuild")]
    public async Task<IActionResult> Rebuild(string name, CancellationToken ct)
    {
        var ok = await indexService.Rebuild(name, ct);
        return ok ? Ok(new { detail = "Rebuild started" }) : NotFound(new { detail = $"Index '{name}' not found" });
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name, CancellationToken ct)
    {
        var ok = await indexService.Delete(name, ct);
        return ok ? Ok(new { detail = "Index deleted" }) : NotFound(new { detail = $"Index '{name}' not found" });
    }

    [HttpPost("{name}/search")]
    public IActionResult Search(string name, [FromBody] IndexSearchRequest request, CancellationToken ct)
    {
        var result = indexService.Search(name, request.Query, request.MaxResults, ct);
        return result is null ? NotFound(new { detail = $"Index '{name}' not found" }) : Ok(result);
    }
}
