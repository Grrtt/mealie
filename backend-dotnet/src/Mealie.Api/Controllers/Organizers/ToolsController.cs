using Mealie.Application.Commands.Organizers;
using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Organizers;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Organizers;

[ApiController]
[Route("api/organizers/tools")]
public class ToolsController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ToolResponse>>> GetTools(
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetToolsQuery(CurrentGroupId, pagination, search), ct));
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ToolResponse>> GetTool(string slug, CancellationToken ct)
    {
        var tool = await executor.ExecuteAsync(new GetToolBySlugQuery(CurrentGroupId, slug), ct);
        if (tool is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(tool);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ToolResponse>> GetToolBySlug(string slug, CancellationToken ct)
    {
        return await GetTool(slug, ct);
    }

    [HttpGet("{id:guid}/recipes")]
    public async Task<ActionResult<IList<RecipeSummaryResponse>>> GetToolRecipes(Guid id, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetRecipesByToolQuery(CurrentGroupId, id), ct));
    }

    [HttpPost]
    public async Task<ActionResult<ToolResponse>> CreateTool([FromBody] CreateToolRequest request, CancellationToken ct)
    {
        var tool = await executor.ExecuteAsync(new CreateToolCommand(CurrentGroupId, request), ct);
        return CreatedAtAction(nameof(GetTool), new { slug = tool.Slug }, tool);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ToolResponse>> UpdateTool(Guid id, [FromBody] UpdateToolRequest request,
        CancellationToken ct)
    {
        var tool = await executor.ExecuteAsync(new UpdateToolCommand(CurrentGroupId, id, request), ct);
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
        var deleted = await executor.ExecuteAsync(new DeleteToolCommand(CurrentGroupId, id), ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }
}
