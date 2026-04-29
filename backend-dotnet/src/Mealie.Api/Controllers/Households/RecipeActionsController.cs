using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/recipe-actions")]
public class RecipeActionsController(ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public ActionResult<PaginatedResponse<object>> GetRecipeActions([FromQuery] PaginationParams pagination)
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
    public ActionResult<object> GetRecipeAction(Guid id)
    {
        return NotFoundOrForbidden();
    }

    [HttpPost]
    public ActionResult<object> CreateRecipeAction([FromBody] object request)
    {
        return Ok(new { id = Guid.NewGuid() });
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public ActionResult<object> UpdateRecipeAction(Guid id, [FromBody] object request)
    {
        return Ok(new { id });
    }

    [HttpDelete("{id:guid}")]
    public IActionResult DeleteRecipeAction(Guid id)
    {
        return NoContent();
    }

    [HttpPost("{id:guid}/trigger/{recipeSlug}")]
    public async Task<IActionResult> TriggerRecipeAction(Guid id, string recipeSlug)
    {
        // Stub: would make HTTP call to configured URL with recipe data
        return Ok(new { triggered = true });
    }
}
