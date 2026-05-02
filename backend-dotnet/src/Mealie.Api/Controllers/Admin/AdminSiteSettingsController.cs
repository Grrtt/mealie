using Mealie.Application.Commands.Admin;
using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Admin;

/// <summary>
///     Admin endpoints for site-wide settings.
///     GET /api/admin/site-settings
///     PUT /api/admin/site-settings
/// </summary>
[ApiController]
[Route("api/admin/site-settings")]
[Authorize(Roles = "admin")]
public class AdminSiteSettingsController(QueryExecutor executor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SiteSettingsResponse>> Get(CancellationToken ct)
    {
        var result = await executor.ExecuteAsync(new GetSiteSettingsQuery(), ct);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<SiteSettingsResponse>> Update(
        [FromBody] UpdateSiteSettingsRequest request, CancellationToken ct)
    {
        var (response, error) = await executor.ExecuteAsync(
            new UpdateSiteSettingsCommand(request), ct);

        if (error is not null)
        {
            return UnprocessableEntity(new { detail = error });
        }

        return Ok(response);
    }
}
