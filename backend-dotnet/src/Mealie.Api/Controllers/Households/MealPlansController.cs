using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Queries;
using Mealie.Application.Queries.MealPlans;
using Mealie.Application.Commands.MealPlans;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/mealplans")]
public class MealPlansController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MealPlanResponse>>> GetMealPlans(
        [FromQuery(Name = "start_date")] DateOnly? startDate,
        [FromQuery(Name = "end_date")] DateOnly? endDate,
        CancellationToken ct)
    {
        var items = await executor.ExecuteAsync(new GetMealPlansQuery(CurrentHouseholdId, startDate, endDate), ct);
        return Ok(new PaginatedResponse<MealPlanResponse>
        {
            Page = 1,
            PerPage = items.Count,
            Total = items.Count,
            TotalPages = 1,
            Items = [.. items]
        });
    }

    [HttpGet("today")]
    public async Task<ActionResult<IList<MealPlanResponse>>> GetTodayMealPlans(CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new GetTodayMealPlansQuery(CurrentHouseholdId), ct));

    [HttpGet("random")]
    public async Task<IActionResult> GetRandomRecipe(
        [FromQuery(Name = "date")] DateOnly? date,
        [FromQuery(Name = "entry_type")] string entryType = "dinner",
        CancellationToken ct = default)
    {
        var queryDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var recipeId = await executor.ExecuteAsync(new GetRandomRecipeIdQuery(CurrentGroupId, queryDate, entryType), ct);
        if (recipeId is null) return NotFound(new { detail = "No recipes available for the given filters." });
        return Ok(new { recipeId });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MealPlanResponse>> GetMealPlan(Guid id, CancellationToken ct)
    {
        var plan = await executor.ExecuteAsync(new GetMealPlanByIdQuery(CurrentHouseholdId, id), ct);
        if (plan is null) return NotFoundOrForbidden();
        return Ok(plan);
    }

    [HttpPost("random")]
    public async Task<ActionResult<MealPlanResponse>> CreateRandomMealPlan(
        [FromBody] CreateRandomMealPlanRequest request, CancellationToken ct)
    {
        var plan = await executor.ExecuteAsync(
            new CreateRandomMealPlanCommand(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request), ct);
        if (plan is null) return BadRequest("No recipes available to pick from.");
        return Ok(plan);
    }

    [HttpPost("fill-day")]
    public async Task<ActionResult<IList<MealPlanResponse>>> FillDay([FromBody] FillDayRequest request,
        CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new FillDayCommand(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request), ct));

    [HttpPost("fill-week")]
    public async Task<ActionResult<IList<MealPlanResponse>>> FillWeek([FromBody] FillWeekRequest request,
        CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new FillWeekCommand(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request), ct));

    [HttpPost]
    public async Task<ActionResult<MealPlanResponse>> CreateMealPlan([FromBody] CreateMealPlanRequest request,
        CancellationToken ct)
    {
        var plan = await executor.ExecuteAsync(
            new CreateMealPlanCommand(CurrentGroupId, CurrentHouseholdId, CurrentUserId, request), ct);
        return CreatedAtAction(nameof(GetMealPlan), new { id = plan.Id }, plan);
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<MealPlanResponse>> UpdateMealPlan(Guid id, [FromBody] UpdateMealPlanRequest request,
        CancellationToken ct)
    {
        var plan = await executor.ExecuteAsync(new UpdateMealPlanCommand(CurrentHouseholdId, id, request), ct);
        if (plan is null) return NotFoundOrForbidden();
        return Ok(plan);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMealPlan(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteMealPlanCommand(CurrentHouseholdId, id), ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }
}
