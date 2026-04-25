using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Application.Services.ShoppingLists;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Households;

[ApiController]
[Route("api/households/shopping/lists")]
public class ShoppingListsController(IShoppingListService shoppingListService, ITenantContext tenantContext) : MealieControllerBase(tenantContext)
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ShoppingListSummaryResponse>>> GetShoppingLists(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
        => Ok(await shoppingListService.GetShoppingListsAsync(CurrentHouseholdId, pagination, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShoppingListResponse>> GetShoppingList(Guid id, CancellationToken ct)
    {
        var list = await shoppingListService.GetByIdAsync(CurrentHouseholdId, id, ct);
        if (list is null) return NotFoundOrForbidden();
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ShoppingListResponse>> CreateShoppingList([FromBody] CreateShoppingListRequest request, CancellationToken ct)
    {
        var list = await shoppingListService.CreateAsync(CurrentGroupId, CurrentHouseholdId, request, ct);
        return CreatedAtAction(nameof(GetShoppingList), new { id = list.Id }, list);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShoppingListResponse>> UpdateShoppingList(Guid id, [FromBody] UpdateShoppingListRequest request, CancellationToken ct)
    {
        var list = await shoppingListService.UpdateAsync(CurrentHouseholdId, id, request, ct);
        if (list is null) return NotFoundOrForbidden();
        return Ok(list);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteShoppingList(Guid id, CancellationToken ct)
    {
        var deleted = await shoppingListService.DeleteAsync(CurrentHouseholdId, id, ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<ShoppingListItemResponse>> AddItem(Guid id, [FromBody] CreateShoppingListItemRequest request, CancellationToken ct)
    {
        var item = await shoppingListService.AddItemAsync(CurrentHouseholdId, id, request, ct);
        return Ok(item);
    }

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ShoppingListItemResponse>> UpdateItem(Guid id, Guid itemId, [FromBody] UpdateShoppingListItemRequest request, CancellationToken ct)
    {
        var item = await shoppingListService.UpdateItemAsync(CurrentHouseholdId, id, itemId, request, ct);
        if (item is null) return NotFoundOrForbidden();
        return Ok(item);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId, CancellationToken ct)
    {
        var deleted = await shoppingListService.DeleteItemAsync(CurrentHouseholdId, id, itemId, ct);
        if (!deleted) return NotFoundOrForbidden();
        return NoContent();
    }
}
