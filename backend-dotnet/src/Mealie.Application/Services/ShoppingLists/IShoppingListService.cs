using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Services.ShoppingLists;

public interface IShoppingListService
{
    Task<PaginatedResponse<ShoppingListSummaryResponse>> GetShoppingListsAsync(Guid householdId, PaginationParams pagination, CancellationToken ct = default);
    Task<ShoppingListResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default);
    Task<ShoppingListResponse> CreateAsync(Guid groupId, Guid householdId, CreateShoppingListRequest request, CancellationToken ct = default);
    Task<ShoppingListResponse?> UpdateAsync(Guid householdId, Guid id, UpdateShoppingListRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default);
    Task<ShoppingListItemResponse> AddItemAsync(Guid householdId, Guid listId, CreateShoppingListItemRequest request, CancellationToken ct = default);
    Task<ShoppingListItemResponse?> UpdateItemAsync(Guid householdId, Guid listId, Guid itemId, UpdateShoppingListItemRequest request, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(Guid householdId, Guid listId, Guid itemId, CancellationToken ct = default);
}
