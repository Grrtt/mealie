using Mealie.Application.Commands.RecipeActions;
using Mealie.Application.Dtos.RecipeActions;
using Mealie.Application.Queries;
using Mealie.Application.Queries.RecipeActions;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/recipe-actions")]
public class RecipeActionsController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<IActionResult> GetRecipeActions([FromQuery] int page = 1, [FromQuery] int perPage = 200,
        CancellationToken ct = default)
    {
        var items = await executor.ExecuteAsync(new GetRecipeActionsQuery(CurrentHouseholdId), ct);
        var total = items.Count;
        var paged = items.Skip((page - 1) * perPage).Take(perPage).ToList();
        return Ok(new
        {
            page,
            perPage,
            total,
            totalPages = (int)Math.Ceiling((double)total / perPage),
            items = paged
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRecipeAction(Guid id, CancellationToken ct = default)
    {
        var item = await executor.ExecuteAsync(new GetRecipeActionByIdQuery(CurrentHouseholdId, id), ct);
        if (item is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRecipeAction([FromBody] CreateRecipeActionRequest request,
        CancellationToken ct = default)
    {
        return Ok(
            await executor.ExecuteAsync(new CreateRecipeActionCommand(CurrentGroupId, CurrentHouseholdId, request), ct));
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateRecipeAction(Guid id, [FromBody] UpdateRecipeActionRequest request,
        CancellationToken ct = default)
    {
        var item = await executor.ExecuteAsync(new UpdateRecipeActionCommand(CurrentHouseholdId, id, request), ct);
        if (item is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRecipeAction(Guid id, CancellationToken ct = default)
    {
        var success = await executor.ExecuteAsync(new DeleteRecipeActionCommand(CurrentHouseholdId, id), ct);
        if (!success)
        {
            return NotFoundOrForbidden();
        }

        return Ok();
    }

    [HttpPost("{id:guid}/trigger/{recipeSlug}")]
    public async Task<IActionResult> TriggerRecipeAction(Guid id, string recipeSlug,
        [FromBody] RecipeActionTriggerRequest? request = null, CancellationToken ct = default)
    {
        var result = await executor.ExecuteAsync(
            new TriggerRecipeActionCommand(CurrentHouseholdId, CurrentGroupId, id, recipeSlug, request), ct);
        if (result is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(result);
    }
}
