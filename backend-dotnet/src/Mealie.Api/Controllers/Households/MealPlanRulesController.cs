using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/mealplans/rules")]
public class MealPlanRulesController(ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public ActionResult<PaginatedResponse<object>> GetMealPlanRules([FromQuery] PaginationParams pagination)
    {
        var response = new PaginatedResponse<object>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = 0,
            TotalPages = 0,
            Items = []
        };
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<object> GetMealPlanRule(Guid id)
    {
        return NotFoundOrForbidden();
    }

    [HttpPost]
    public ActionResult<object> CreateMealPlanRule([FromBody] object request)
    {
        return Ok(new { id = Guid.NewGuid() });
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public ActionResult<object> UpdateMealPlanRule(Guid id, [FromBody] object request)
    {
        return Ok(new { id });
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteMealPlanRule(Guid id)
    {
        return NoContent();
    }
}
