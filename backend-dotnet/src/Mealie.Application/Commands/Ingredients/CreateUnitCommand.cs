using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record CreateUnitCommand(Guid GroupId, CreateUnitRequest Request) : IQuery<UnitResponse>
{
    public async Task<UnitResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var unit = new IngredientUnit
        {
            Id = Guid.NewGuid(), Name = Request.Name, Description = Request.Description,
            Abbreviation = Request.Abbreviation, PluralName = Request.PluralName,
            PluralAbbreviation = Request.PluralAbbreviation,
            UseAbbreviation = Request.UseAbbreviation, Fraction = Request.Fraction,
            GroupId = GroupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        foreach (var alias in Request.Aliases)
            unit.Aliases.Add(new IngredientUnitAlias { Id = Guid.NewGuid(), Name = alias, UnitId = unit.Id });

        db.Units.Add(unit);
        await db.SaveChangesAsync(ct);
        return UnitMappings.MapToResponse(unit);
    }
}

file static class UnitMappings
{
    public static UnitResponse MapToResponse(IngredientUnit u) =>
        new()
        {
            Id = u.Id, Name = u.Name, Description = u.Description, Abbreviation = u.Abbreviation,
            PluralName = u.PluralName, PluralAbbreviation = u.PluralAbbreviation,
            UseAbbreviation = u.UseAbbreviation, Fraction = u.Fraction,
            GroupId = u.GroupId, CreatedAt = u.CreatedAt, UpdateAt = u.UpdateAt,
            Aliases = u.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList()
        };
}