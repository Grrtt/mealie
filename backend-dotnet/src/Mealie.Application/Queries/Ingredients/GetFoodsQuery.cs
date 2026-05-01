using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Ingredients;

public record GetFoodsQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<FoodResponse>>
{
    public async Task<PaginatedResponse<FoodResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.Foods.IgnoreQueryFilters().Include(f => f.Aliases).Include(f => f.Label)
            .Where(f => f.GroupId == GroupId);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(f => f.Name.Contains(Search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(f => f.Name).Skip(Pagination.Skip).Take(Pagination.PerPage).ToListAsync(ct);
        return new PaginatedResponse<FoodResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage),
            Items = items.Select(FoodMappings.MapToResponse).ToList()
        };
    }
}

file static class FoodMappings
{
    public static FoodResponse MapToResponse(IngredientFood f)
    {
        return new FoodResponse
        {
            Id = f.Id, Name = f.Name, Description = f.Description, PluralName = f.PluralName,
            UnitId = f.UnitId, LabelId = f.LabelId,
            Label = f.Label is null
                ? null
                : new LabelSummaryResponse { Id = f.Label.Id, Name = f.Label.Name, Color = f.Label.Color },
            GroupId = f.GroupId, OnHand = f.OnHand,
            Aliases = f.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList(),
            CreatedAt = f.CreatedAt, UpdateAt = f.UpdateAt
        };
    }
}
