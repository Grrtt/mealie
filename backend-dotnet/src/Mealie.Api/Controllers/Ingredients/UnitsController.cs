using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Ingredients;
using Mealie.Application.Commands.Ingredients;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Ingredients;

[ApiController]
[Route("api/units")]
public class UnitsController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UnitResponse>>> GetUnits(
        [FromQuery] PaginationParams pagination, [FromQuery] string? search, CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new GetUnitsQuery(CurrentGroupId, pagination, search), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitResponse>> GetUnit(Guid id, CancellationToken ct)
    {
        var unit = await executor.ExecuteAsync(new GetUnitByIdQuery(CurrentGroupId, id), ct);
        if (unit is null) return NotFoundOrForbidden();
        return Ok(unit);
    }

    [HttpPost]
    public async Task<ActionResult<UnitResponse>> CreateUnit([FromBody] CreateUnitRequest request, CancellationToken ct)
    {
        var unit = await executor.ExecuteAsync(new CreateUnitCommand(CurrentGroupId, request), ct);
        return CreatedAtAction(nameof(GetUnit), new { id = unit.Id }, unit);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UnitResponse>> UpdateUnit(Guid id, [FromBody] UpdateUnitRequest request,
        CancellationToken ct)
    {
        var unit = await executor.ExecuteAsync(new UpdateUnitCommand(CurrentGroupId, id, request), ct);
        if (unit is null) return NotFoundOrForbidden();
        return Ok(unit);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<UnitResponse>> PatchUnit(Guid id, [FromBody] UpdateUnitRequest request,
        CancellationToken ct) => await UpdateUnit(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteUnitCommand(CurrentGroupId, id), ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }

    [HttpPut("merge")]
    public async Task<IActionResult> MergeUnits([FromBody] MergeUnitRequest request, CancellationToken ct)
    {
        var success = await executor.ExecuteAsync(new MergeUnitCommand(CurrentGroupId, request.FromUnit, request.ToUnit), ct);
        if (!success) return NotFoundOrForbidden();
        return Ok(new { detail = "Units merged successfully" });
    }
}

public class MergeUnitRequest
{
    public Guid FromUnit { get; set; }
    public Guid ToUnit { get; set; }
}
