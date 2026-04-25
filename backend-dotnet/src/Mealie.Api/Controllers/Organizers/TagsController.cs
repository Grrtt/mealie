using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Organizers;

[ApiController]
[Route("api/organizers/tags")]
public class TagsController(IOrganizerService organizerService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TagResponse>>> GetTags(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
        => Ok(await organizerService.GetTagsAsync(CurrentGroupId, pagination, ct));

    [HttpGet("{slug}")]
    public async Task<ActionResult<TagResponse>> GetTag(string slug, CancellationToken ct)
    {
        var tag = await organizerService.GetTagBySlugAsync(CurrentGroupId, slug, ct);
        if (tag is null) return NotFoundOrForbidden();
        return Ok(tag);
    }

    [HttpPost]
    public async Task<ActionResult<TagResponse>> CreateTag([FromBody] CreateOrganizerRequest request, CancellationToken ct)
    {
        var tag = await organizerService.CreateTagAsync(CurrentGroupId, request, ct);
        return CreatedAtAction(nameof(GetTag), new { slug = tag.Slug }, tag);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TagResponse>> UpdateTag(Guid id, [FromBody] UpdateOrganizerRequest request, CancellationToken ct)
    {
        var tag = await organizerService.UpdateTagAsync(CurrentGroupId, id, request, ct);
        if (tag is null) return NotFoundOrForbidden();
        return Ok(tag);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTag(Guid id, CancellationToken ct)
    {
        var deleted = await organizerService.DeleteTagAsync(CurrentGroupId, id, ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }
}
