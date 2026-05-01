using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Ingredients;

public record GetUnitByIdQuery(Guid GroupId, Guid Id) : IQuery<UnitResponse?>
{
    public async Task<UnitResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var u = await services.Db.Units.IgnoreQueryFilters()
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.GroupId == GroupId && u.Id == Id, ct);
        return u is null ? null : UnitMappings.MapToResponse(u);
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
