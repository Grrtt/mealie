using Mealie.Application.Dtos.MealPlans;
using Mealie.Application.Services.MealPlans;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/mealplans/rules")]
public class MealPlanRulesController(IMealPlanRuleService mealPlanRuleService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MealPlanRuleResponse>>> GetMealPlanRules([FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var items = await mealPlanRuleService.GetAllAsync(CurrentGroupId, CurrentHouseholdId, ct);
        var paged = items.Skip((pagination.Page - 1) * pagination.PerPage).Take(pagination.PerPage).ToList();
        return Ok(new PaginatedResponse<MealPlanRuleResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = items.Count,
            TotalPages = (int)Math.Ceiling(items.Count / (double)pagination.PerPage),
            Items = paged,
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MealPlanRuleResponse>> GetMealPlanRule(Guid id, CancellationToken ct)
    {
        var rule = await mealPlanRuleService.GetByIdAsync(CurrentGroupId, id, ct);
        if (rule is null) return NotFoundOrForbidden();
        return Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<MealPlanRuleResponse>> CreateMealPlanRule([FromBody] CreateMealPlanRuleRequest request, CancellationToken ct)
    {
        var rule = await mealPlanRuleService.CreateAsync(CurrentGroupId, CurrentHouseholdId, request, ct);
        return CreatedAtAction(nameof(GetMealPlanRule), new { id = rule.Id }, rule);
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<MealPlanRuleResponse>> UpdateMealPlanRule(Guid id, [FromBody] UpdateMealPlanRuleRequest request, CancellationToken ct)
    {
        var rule = await mealPlanRuleService.UpdateAsync(CurrentGroupId, id, request, ct);
        if (rule is null) return NotFoundOrForbidden();
        return Ok(rule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMealPlanRule(Guid id, CancellationToken ct)
    {
        var deleted = await mealPlanRuleService.DeleteAsync(CurrentGroupId, id, ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }
}

