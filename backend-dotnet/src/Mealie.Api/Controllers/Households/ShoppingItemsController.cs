using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/shopping/items")]
public class ShoppingItemsController(IShoppingListService shoppingListService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ShoppingListItemResponse>>> GetItems(
        [FromQuery] PaginationParams pagination,
        [FromQuery] bool? checked_,
        CancellationToken ct)
        => Ok(await shoppingListService.GetItemsAsync(CurrentHouseholdId, pagination, checked_, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShoppingListItemResponse>> GetItem(Guid id, CancellationToken ct)
    {
        var item = await shoppingListService.GetItemByIdAsync(CurrentHouseholdId, id, ct);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingListItemResponse>> CreateItem([FromBody] CreateShoppingListItemWithListRequest request, CancellationToken ct)
    {
        var item = await shoppingListService.CreateStandaloneItemAsync(CurrentHouseholdId, new CreateShoppingListItemRequest
        {
            Note = request.Note,
            IsFood = request.IsFood,
            DisableAmount = request.DisableAmount,
            Quantity = request.Quantity,
            UnitId = request.UnitId,
            FoodId = request.FoodId,
            LabelId = request.LabelId,
        }, request.ListId, ct);
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShoppingListItemResponse>> UpdateItem(Guid id, [FromBody] UpdateShoppingListItemRequest request, CancellationToken ct)
    {
        var item = await shoppingListService.UpdateStandaloneItemAsync(CurrentHouseholdId, id, request, ct);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<ShoppingListItemResponse>> PatchItem(Guid id, [FromBody] UpdateShoppingListItemRequest request, CancellationToken ct)
    {
        var item = await shoppingListService.UpdateStandaloneItemAsync(CurrentHouseholdId, id, request, ct);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        var deleted = await shoppingListService.DeleteStandaloneItemAsync(CurrentHouseholdId, id, ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }

    [HttpPost("create-bulk")]
    public async Task<ActionResult<IList<ShoppingListItemResponse>>> CreateBulkItems([FromBody] BulkCreateShoppingListItemRequest request, CancellationToken ct)
    {
        var items = await shoppingListService.CreateBulkItemsAsync(CurrentHouseholdId, request, ct);
        return Ok(items);
    }

    [HttpPut]
    public async Task<ActionResult<IList<ShoppingListItemResponse>>> UpdateBulkItems([FromBody] BulkUpdateShoppingListItemRequest request, CancellationToken ct)
    {
        var items = await shoppingListService.UpdateBulkItemsAsync(CurrentHouseholdId, request, ct);
        return Ok(items);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBulkItems([FromBody] BulkDeleteShoppingListItemRequest request, CancellationToken ct)
    {
        var deleted = await shoppingListService.DeleteBulkItemsAsync(CurrentHouseholdId, request, ct);
        if (!deleted) return BadRequest(new { detail = "No items were deleted" });
        return NoContent();
    }
}
