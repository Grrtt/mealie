using Mealie.Application.Dtos.Groups;
using Mealie.Application.Services.Groups;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Groups;

[ApiController]
[Route("api/groups/labels")]
public class GroupLabelsController(IGroupLabelService groupLabelService, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<GroupLabelResponse>>> GetLabels(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        return Ok(await groupLabelService.GetLabelsAsync(CurrentGroupId, pagination, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GroupLabelResponse>> GetLabel(Guid id, CancellationToken ct)
    {
        var label = await groupLabelService.GetByIdAsync(CurrentGroupId, id, ct);
        if (label is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(label);
    }

    [HttpPost]
    public async Task<ActionResult<GroupLabelResponse>> CreateLabel([FromBody] CreateGroupLabelRequest request,
        CancellationToken ct)
    {
        var label = await groupLabelService.CreateAsync(CurrentGroupId, request, ct);
        return CreatedAtAction(nameof(GetLabel), new { id = label.Id }, label);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GroupLabelResponse>> UpdateLabel(Guid id, [FromBody] UpdateGroupLabelRequest request,
        CancellationToken ct)
    {
        var label = await groupLabelService.UpdateAsync(CurrentGroupId, id, request, ct);
        if (label is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(label);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<GroupLabelResponse>> PatchLabel(Guid id, [FromBody] UpdateGroupLabelRequest request,
        CancellationToken ct)
    {
        var label = await groupLabelService.UpdateAsync(CurrentGroupId, id, request, ct);
        if (label is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(label);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLabel(Guid id, CancellationToken ct)
    {
        var deleted = await groupLabelService.DeleteAsync(CurrentGroupId, id, ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }
}
