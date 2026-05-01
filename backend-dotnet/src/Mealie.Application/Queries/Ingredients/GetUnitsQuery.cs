using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Ingredients;

public record GetUnitsQuery(Guid GroupId, PaginationParams Pagination, string? Search = null)
    : IQuery<PaginatedResponse<UnitResponse>>
{
    public async Task<PaginatedResponse<UnitResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.Units.IgnoreQueryFilters().Include(u => u.Aliases).Where(u => u.GroupId == GroupId);
        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(u => u.Name.Contains(Search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.Name).Skip(Pagination.Skip).Take(Pagination.PerPage).ToListAsync(ct);
        return new PaginatedResponse<UnitResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage),
            Items = items.Select(UnitMappings.MapToResponse).ToList()
        };
    }
}

file static class UnitMappings
{
    public static UnitResponse MapToResponse(IngredientUnit u)
    {
        return new UnitResponse
        {
            Id = u.Id, Name = u.Name, Description = u.Description, Abbreviation = u.Abbreviation,
            PluralName = u.PluralName, PluralAbbreviation = u.PluralAbbreviation,
            UseAbbreviation = u.UseAbbreviation, Fraction = u.Fraction,
            GroupId = u.GroupId, CreatedAt = u.CreatedAt, UpdateAt = u.UpdateAt,
            Aliases = u.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList()
        };
    }
}
