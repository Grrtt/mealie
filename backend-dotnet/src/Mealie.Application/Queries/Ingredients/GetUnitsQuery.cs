using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Services.Ingredients;
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
        return await IngredientCrudCore.GetUnitsAsync(services.Db, GroupId, Pagination, Search, ct);
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
