using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/mealplans")]
public class MealPlansController(IMealPlanService mealPlanService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MealPlanResponse>>> GetMealPlans(
        [FromQuery(Name = "start_date")] DateOnly? startDate,
        [FromQuery(Name = "end_date")] DateOnly? endDate,
        CancellationToken ct)
    {
        var items = await mealPlanService.GetMealPlansAsync(CurrentHouseholdId, startDate, endDate, ct);
        return Ok(new PaginatedResponse<MealPlanResponse>
        {
            Page = 1,
            PerPage = items.Count,
            Total = items.Count,
            TotalPages = 1,
            Items = [.. items],
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MealPlanResponse>> GetMealPlan(Guid id, CancellationToken ct)
    {
        var plan = await mealPlanService.GetByIdAsync(CurrentHouseholdId, id, ct);
        if (plan is null) return NotFoundOrForbidden();
        return Ok(plan);
    }

    [HttpPost("random")]
    public async Task<ActionResult<MealPlanResponse>> CreateRandomMealPlan([FromBody] CreateRandomMealPlanRequest request, CancellationToken ct)
    {
        var plan = await mealPlanService.CreateRandomAsync(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request, ct);
        if (plan is null) return BadRequest("No recipes available to pick from.");
        return Ok(plan);
    }

    [HttpPost("fill-day")]
    public async Task<ActionResult<IList<MealPlanResponse>>> FillDay([FromBody] FillDayRequest request, CancellationToken ct)
    {
        var plans = await mealPlanService.FillDayAsync(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request, ct);
        return Ok(plans);
    }

    [HttpPost("fill-week")]
    public async Task<ActionResult<IList<MealPlanResponse>>> FillWeek([FromBody] FillWeekRequest request, CancellationToken ct)
    {
        var plans = await mealPlanService.FillWeekAsync(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request, ct);
        return Ok(plans);
    }

    [HttpPost]
    public async Task<ActionResult<MealPlanResponse>> CreateMealPlan([FromBody] CreateMealPlanRequest request, CancellationToken ct)
    {
        var plan = await mealPlanService.CreateAsync(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetMealPlan), new { id = plan.Id }, plan);
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<MealPlanResponse>> UpdateMealPlan(Guid id, [FromBody] UpdateMealPlanRequest request, CancellationToken ct)
    {
        var plan = await mealPlanService.UpdateAsync(CurrentHouseholdId, id, request, ct);
        if (plan is null) return NotFoundOrForbidden();
        return Ok(plan);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMealPlan(Guid id, CancellationToken ct)
    {
        var deleted = await mealPlanService.DeleteAsync(CurrentHouseholdId, id, ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }
}
