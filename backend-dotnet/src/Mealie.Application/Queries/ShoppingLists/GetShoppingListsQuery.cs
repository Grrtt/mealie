using Mealie.Application.Dtos.ShoppingLists;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.ShoppingLists;

public record GetShoppingListsQuery(Guid HouseholdId, PaginationParams Pagination)
    : IQuery<PaginatedResponse<ShoppingListSummaryResponse>>
{
    public async Task<PaginatedResponse<ShoppingListSummaryResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.ShoppingLists.IgnoreQueryFilters().Where(s => s.HouseholdId == HouseholdId);
        var total = await query.CountAsync(ct);
        var pagedQuery = query.OrderBy(s => s.Name);
        var itemsQuery = Pagination.PerPage > 0
            ? pagedQuery.Skip(Pagination.Skip).Take(Pagination.PerPage)
            : pagedQuery;
        var items = await itemsQuery
            .Select(s => new ShoppingListSummaryResponse
            {
                Id = s.Id, Name = s.Name, GroupId = s.GroupId, HouseholdId = s.HouseholdId,
                UserId = s.UserId, RecipeReferenceCount = s.RecipeReferences.Count,
                CreatedAt = s.CreatedAt, UpdateAt = s.UpdateAt
            })
            .ToListAsync(ct);
        var perPage = Pagination.PerPage > 0 ? Pagination.PerPage : Math.Max(total, 1);
        return new PaginatedResponse<ShoppingListSummaryResponse>
        {
            Page = Pagination.PerPage > 0 ? Pagination.Page : 1,
            PerPage = perPage,
            Total = total,
            TotalPages = perPage > 0 ? (int)Math.Ceiling((double)total / perPage) : 1,
            Items = items
        };
    }
}
