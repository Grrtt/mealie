using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Organizers;
using Mealie.Application.Commands.Organizers;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Organizers;

[ApiController]
[Route("api/organizers/tags")]
public class TagsController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TagResponse>>> GetTags(
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new GetTagsQuery(CurrentGroupId, pagination, search), ct));

    [HttpGet("empty")]
    public async Task<ActionResult<IList<TagResponse>>> GetEmptyTags(CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new GetEmptyTagsQuery(CurrentGroupId), ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<TagResponse>> GetTag(string slug, CancellationToken ct)
    {
        var tag = await executor.ExecuteAsync(new GetTagBySlugQuery(CurrentGroupId, slug), ct);
        if (tag is null) return NotFoundOrForbidden();
        return Ok(tag);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<TagResponse>> GetTagBySlug(string slug, CancellationToken ct)
        => await GetTag(slug, ct);

    [HttpGet("{id:guid}/recipes")]
    public async Task<ActionResult<IList<RecipeSummaryResponse>>> GetTagRecipes(Guid id, CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new GetRecipesByTagQuery(CurrentGroupId, id), ct));

    [HttpPost]
    public async Task<ActionResult<TagResponse>> CreateTag([FromBody] CreateOrganizerRequest request,
        CancellationToken ct)
    {
        var tag = await executor.ExecuteAsync(new CreateTagCommand(CurrentGroupId, request), ct);
        return CreatedAtAction(nameof(GetTag), new { slug = tag.Slug }, tag);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TagResponse>> UpdateTag(Guid id, [FromBody] UpdateOrganizerRequest request,
        CancellationToken ct)
    {
        var tag = await executor.ExecuteAsync(new UpdateTagCommand(CurrentGroupId, id, request), ct);
        if (tag is null) return NotFoundOrForbidden();
        return Ok(tag);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<TagResponse>> PatchTag(Guid id, [FromBody] UpdateOrganizerRequest request,
        CancellationToken ct) => await UpdateTag(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteTagCommand(CurrentGroupId, id), ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }
}
