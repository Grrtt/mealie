using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/mealplans")]
public class MealPlansController(IMealPlanService mealPlanService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<IList<MealPlanResponse>>> GetMealPlans(
        [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken ct)
        => Ok(await mealPlanService.GetMealPlansAsync(CurrentHouseholdId, startDate, endDate, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MealPlanResponse>> GetMealPlan(Guid id, CancellationToken ct)
    {
        var plan = await mealPlanService.GetByIdAsync(CurrentHouseholdId, id, ct);
        if (plan is null) return NotFoundOrForbidden();
        return Ok(plan);
    }

    [HttpPost]
    public async Task<ActionResult<MealPlanResponse>> CreateMealPlan([FromBody] CreateMealPlanRequest request, CancellationToken ct)
    {
        var plan = await mealPlanService.CreateAsync(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetMealPlan), new { id = plan.Id }, plan);
    }

    [HttpPut("{id:guid}")]
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
