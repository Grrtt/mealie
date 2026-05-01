using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Organizers;

[ApiController]
[Route("api/organizers/tools")]
public class ToolsController(IOrganizerService organizerService, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ToolResponse>>> GetTools(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        return Ok(await organizerService.GetToolsAsync(CurrentGroupId, pagination, ct));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ToolResponse>> GetTool(string slug, CancellationToken ct)
    {
        var tool = await organizerService.GetToolBySlugAsync(CurrentGroupId, slug, ct);
        if (tool is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(tool);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ToolResponse>> GetToolBySlug(string slug, CancellationToken ct)
    {
        var tool = await organizerService.GetToolBySlugAsync(CurrentGroupId, slug, ct);
        if (tool is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(tool);
    }

    [HttpPost]
    public async Task<ActionResult<ToolResponse>> CreateTool([FromBody] CreateToolRequest request, CancellationToken ct)
    {
        var tool = await organizerService.CreateToolAsync(CurrentGroupId, request, ct);
        return CreatedAtAction(nameof(GetTool), new { slug = tool.Slug }, tool);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ToolResponse>> UpdateTool(Guid id, [FromBody] UpdateToolRequest request,
        CancellationToken ct)
    {
        var tool = await organizerService.UpdateToolAsync(CurrentGroupId, id, request, ct);
        if (tool is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(tool);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ToolResponse>> PatchTool(Guid id, [FromBody] UpdateToolRequest request,
        CancellationToken ct)
    {
        return await UpdateTool(id, request, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTool(Guid id, CancellationToken ct)
    {
        var deleted = await organizerService.DeleteToolAsync(CurrentGroupId, id, ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }
}
