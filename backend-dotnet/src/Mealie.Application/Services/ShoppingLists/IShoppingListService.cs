using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Shared.Pagination;

namespace Mealie.Application.Services.ShoppingLists;

public interface IShoppingListService
{
    Task<PaginatedResponse<ShoppingListSummaryResponse>> GetShoppingListsAsync(Guid householdId,
        PaginationParams pagination, CancellationToken ct = default);

    Task<ShoppingListResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default);

    Task<ShoppingListResponse> CreateAsync(Guid groupId, Guid householdId, CreateShoppingListRequest request,
        CancellationToken ct = default);

    Task<ShoppingListResponse?> UpdateAsync(Guid householdId, Guid id, UpdateShoppingListRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default);

    Task<ShoppingListItemResponse> AddItemAsync(Guid householdId, Guid listId, CreateShoppingListItemRequest request,
        CancellationToken ct = default);

    Task<ShoppingListItemResponse?> UpdateItemAsync(Guid householdId, Guid listId, Guid itemId,
        UpdateShoppingListItemRequest request, CancellationToken ct = default);

    Task<bool> DeleteItemAsync(Guid householdId, Guid listId, Guid itemId, CancellationToken ct = default);

    // Standalone items API
    Task<PaginatedResponse<ShoppingListItemResponse>> GetItemsAsync(Guid householdId, PaginationParams pagination,
        bool? checked_ = null, CancellationToken ct = default);

    Task<ShoppingListItemResponse?> GetItemByIdAsync(Guid householdId, Guid itemId, CancellationToken ct = default);

    Task<ShoppingListItemResponse> CreateStandaloneItemAsync(Guid householdId, CreateShoppingListItemRequest request,
        Guid listId, CancellationToken ct = default);

    Task<ShoppingListItemResponse?> UpdateStandaloneItemAsync(Guid householdId, Guid itemId,
        UpdateShoppingListItemRequest request, CancellationToken ct = default);

    Task<bool> DeleteStandaloneItemAsync(Guid householdId, Guid itemId, CancellationToken ct = default);

    Task<IList<ShoppingListItemResponse>> CreateBulkItemsAsync(Guid householdId,
        BulkCreateShoppingListItemRequest request, CancellationToken ct = default);

    Task<IList<ShoppingListItemResponse>> UpdateBulkItemsAsync(Guid householdId,
        BulkUpdateShoppingListItemRequest request, CancellationToken ct = default);

    Task<bool> DeleteBulkItemsAsync(Guid householdId, BulkDeleteShoppingListItemRequest request,
        CancellationToken ct = default);

    // Recipe linking
    Task<ShoppingListResponse?> AddRecipeAsync(Guid householdId, Guid listId, AddRecipeToShoppingListRequest request,
        CancellationToken ct = default);

    Task<bool> RemoveRecipeAsync(Guid householdId, Guid listId, Guid recipeId, CancellationToken ct = default);

    Task<ShoppingListResponse?> UpdateLabelSettingsAsync(Guid householdId, Guid listId,
        UpdateShoppingListLabelSettingsRequest request, CancellationToken ct = default);
}
