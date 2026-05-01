using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Queries;
using Mealie.Application.Queries.ShoppingLists;
using Mealie.Application.Commands.ShoppingLists;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/shopping/items")]
public class ShoppingItemsController(QueryExecutor executor, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ShoppingListItemResponse>>> GetItems(
        [FromQuery] PaginationParams pagination,
        [FromQuery] bool? checked_,
        CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new GetShoppingListItemsQuery(CurrentHouseholdId, pagination, checked_), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShoppingListItemResponse>> GetItem(Guid id, CancellationToken ct)
    {
        var item = await executor.ExecuteAsync(new GetShoppingListItemByIdQuery(CurrentHouseholdId, id), ct);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingListItemResponse>> CreateItem(
        [FromBody] CreateShoppingListItemWithListRequest request, CancellationToken ct)
    {
        var item = await executor.ExecuteAsync(new CreateStandaloneItemCommand(
            CurrentHouseholdId, request.ListId,
            new CreateShoppingListItemRequest
            {
                Note = request.Note,
                IsFood = request.IsFood,
                DisableAmount = request.DisableAmount,
                Quantity = request.Quantity,
                UnitId = request.UnitId,
                FoodId = request.FoodId,
                LabelId = request.LabelId
            }), ct);
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ShoppingListItemResponse>> UpdateItem(Guid id,
        [FromBody] UpdateShoppingListItemRequest request, CancellationToken ct)
    {
        var item = await executor.ExecuteAsync(new UpdateStandaloneItemCommand(CurrentHouseholdId, id, request), ct);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteStandaloneItemCommand(CurrentHouseholdId, id), ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }

    [HttpPost("create-bulk")]
    public async Task<ActionResult<IList<ShoppingListItemResponse>>> CreateBulkItems(
        [FromBody] BulkCreateShoppingListItemRequest request, CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new CreateBulkShoppingListItemsCommand(CurrentHouseholdId, request), ct));

    [HttpPut]
    public async Task<ActionResult<IList<ShoppingListItemResponse>>> UpdateBulkItems(
        [FromBody] BulkUpdateShoppingListItemRequest request, CancellationToken ct)
        => Ok(await executor.ExecuteAsync(new UpdateBulkShoppingListItemsCommand(CurrentHouseholdId, request), ct));

    [HttpDelete]
    public async Task<IActionResult> DeleteBulkItems([FromBody] BulkDeleteShoppingListItemRequest request,
        CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteBulkShoppingListItemsCommand(CurrentHouseholdId, request), ct);
        if (!deleted) return BadRequest(new { detail = "No items were deleted" });
        return NoContent();
    }
}
