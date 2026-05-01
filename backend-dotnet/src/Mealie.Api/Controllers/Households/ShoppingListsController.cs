using Mealie.Application.Commands.ShoppingLists;
using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Application.Queries.ShoppingLists;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/shopping/lists")]
public class ShoppingListsController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ShoppingListSummaryResponse>>> GetShoppingLists(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new GetShoppingListsQuery(CurrentHouseholdId, pagination), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShoppingListResponse>> GetShoppingList(Guid id, CancellationToken ct)
    {
        var list = await executor.ExecuteAsync(new GetShoppingListByIdQuery(CurrentHouseholdId, id), ct);
        if (list is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingListResponse>> CreateShoppingList(
        [FromBody] CreateShoppingListRequest request, CancellationToken ct)
    {
        var list = await executor.ExecuteAsync(
            new CreateShoppingListCommand(CurrentGroupId, CurrentHouseholdId, request), ct);
        return CreatedAtAction(nameof(GetShoppingList), new { id = list.Id }, list);
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ShoppingListResponse>> UpdateShoppingList(Guid id,
        [FromBody] UpdateShoppingListRequest request, CancellationToken ct)
    {
        var list = await executor.ExecuteAsync(new UpdateShoppingListCommand(CurrentHouseholdId, id, request), ct);
        if (list is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(list);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteShoppingList(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteShoppingListCommand(CurrentHouseholdId, id), ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<ShoppingListItemResponse>> AddItem(Guid id,
        [FromBody] CreateShoppingListItemRequest request, CancellationToken ct)
    {
        return Ok(await executor.ExecuteAsync(new AddShoppingListItemCommand(CurrentHouseholdId, id, request), ct));
    }

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ShoppingListItemResponse>> UpdateItem(Guid id, Guid itemId,
        [FromBody] UpdateShoppingListItemRequest request, CancellationToken ct)
    {
        var item = await executor.ExecuteAsync(
            new UpdateShoppingListItemCommand(CurrentHouseholdId, id, itemId, request), ct);
        if (item is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(item);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId, CancellationToken ct)
    {
        var deleted =
            await executor.ExecuteAsync(new DeleteShoppingListItemCommand(CurrentHouseholdId, id, itemId), ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/recipe")]
    public async Task<ActionResult<ShoppingListResponse>> AddRecipe(Guid id,
        [FromBody] AddRecipeToShoppingListRequest request, CancellationToken ct)
    {
        var list = await executor.ExecuteAsync(new AddRecipeToShoppingListCommand(CurrentHouseholdId, id, request), ct);
        if (list is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(list);
    }

    [HttpPost("{id:guid}/recipe/{recipeId:guid}/delete")]
    public async Task<IActionResult> RemoveRecipe(Guid id, Guid recipeId, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(
            new RemoveRecipeFromShoppingListCommand(CurrentHouseholdId, id, recipeId), ct);
        if (!deleted)
        {
            return NotFoundOrForbidden();
        }

        return NoContent();
    }

    [HttpPut("{id:guid}/label-settings")]
    public async Task<ActionResult<ShoppingListResponse>> UpdateLabelSettings(Guid id,
        [FromBody] UpdateShoppingListLabelSettingsRequest request, CancellationToken ct)
    {
        var list = await executor.ExecuteAsync(
            new UpdateShoppingListLabelSettingsCommand(CurrentHouseholdId, id, request), ct);
        if (list is null)
        {
            return NotFoundOrForbidden();
        }

        return Ok(list);
    }
}
