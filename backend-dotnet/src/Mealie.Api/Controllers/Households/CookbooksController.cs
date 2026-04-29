using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Cookbooks;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/cookbooks")]
public class CookbooksController(ICookbookService cookbookService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CookbookResponse>>> GetCookbooks(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
        => Ok(await cookbookService.GetCookbooksAsync(CurrentHouseholdId, pagination, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CookbookResponse>> GetCookbook(Guid id, CancellationToken ct)
    {
        var cookbook = await cookbookService.GetByIdAsync(CurrentHouseholdId, id, ct);
        if (cookbook is null) return NotFoundOrForbidden();
        return Ok(cookbook);
    }

    [HttpPost]
    public async Task<ActionResult<CookbookResponse>> CreateCookbook([FromBody] CreateCookbookRequest request, CancellationToken ct)
    {
        var cookbook = await cookbookService.CreateAsync(CurrentGroupId, CurrentHouseholdId, request, ct);
        return CreatedAtAction(nameof(GetCookbook), new { id = cookbook.Id }, cookbook);
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CookbookResponse>> UpdateCookbook(Guid id, [FromBody] UpdateCookbookRequest request, CancellationToken ct)
    {
        var cookbook = await cookbookService.UpdateAsync(CurrentHouseholdId, id, request, ct);
        if (cookbook is null) return NotFoundOrForbidden();
        return Ok(cookbook);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCookbook(Guid id, CancellationToken ct)
    {
        var deleted = await cookbookService.DeleteAsync(CurrentHouseholdId, id, ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> ReorderCookbooks([FromBody] IEnumerable<CookbookReorderRequest> reorderRequests, CancellationToken ct)
    {
        var success = await cookbookService.ReorderAsync(CurrentHouseholdId, reorderRequests, ct);
        if (!success) return BadRequest();
        return Ok();
    }
}
