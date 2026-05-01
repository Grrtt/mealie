using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Services.Ingredients;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Ingredients;

[ApiController]
[Route("api/units")]
public class UnitsController(IUnitService unitService, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UnitResponse>>> GetUnits(
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
    {
        return Ok(await unitService.GetUnitsAsync(CurrentGroupId, pagination, search, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitResponse>> GetUnit(Guid id, CancellationToken ct)
    {
        var unit = await unitService.GetByIdAsync(CurrentGroupId, id, ct);
        if (unit is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(unit);
    }

    [HttpPost]
    public async Task<ActionResult<UnitResponse>> CreateUnit([FromBody] CreateUnitRequest request, CancellationToken ct)
    {
        var unit = await unitService.CreateAsync(CurrentGroupId, request, ct);
        return CreatedAtAction(nameof(GetUnit), new { id = unit.Id }, unit);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UnitResponse>> UpdateUnit(Guid id, [FromBody] UpdateUnitRequest request,
        CancellationToken ct)
    {
        var unit = await unitService.UpdateAsync(CurrentGroupId, id, request, ct);
        if (unit is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(unit);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UnitResponse>> PatchUnit(Guid id, [FromBody] UpdateUnitRequest request,
        CancellationToken ct)
    {
        return await UpdateUnit(id, request, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid id, CancellationToken ct)
    {
        var deleted = await unitService.DeleteAsync(CurrentGroupId, id, ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }

    [HttpPut("merge")]
    public async Task<IActionResult> MergeUnits([FromBody] MergeUnitRequest request, CancellationToken ct)
    {
        var success = await unitService.MergeAsync(CurrentGroupId, request.FromUnit, request.ToUnit, ct);
        if (!success)
        {
            return NotFoundOrForbidden();
        }

        return Ok(new { detail = "Units merged successfully" });
    }
}

public class MergeUnitRequest
{
    public Guid FromUnit { get; set; }
    public Guid ToUnit { get; set; }
}
