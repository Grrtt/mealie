using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Ingredients;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record UpdateUnitCommand(Guid GroupId, Guid Id, UpdateUnitRequest Request) : IQuery<UnitResponse?>
{
    public async Task<UnitResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var unit = await db.Units.IgnoreQueryFilters()
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.GroupId == GroupId && u.Id == Id, ct);
        if (unit is null)
        {
            return null;
        }

        if (Request.Name is not null)
        {
            unit.Name = Request.Name;
        }

        if (Request.Description is not null)
        {
            unit.Description = Request.Description;
        }

        if (Request.Abbreviation is not null)
        {
            unit.Abbreviation = Request.Abbreviation;
        }

        if (Request.PluralName is not null)
        {
            unit.PluralName = Request.PluralName;
        }

        if (Request.PluralAbbreviation is not null)
        {
            unit.PluralAbbreviation = Request.PluralAbbreviation;
        }

        if (Request.UseAbbreviation.HasValue)
        {
            unit.UseAbbreviation = Request.UseAbbreviation.Value;
        }

        if (Request.Fraction.HasValue)
        {
            unit.Fraction = Request.Fraction.Value;
        }

        if (Request.Aliases is not null)
        {
            unit.Aliases.Clear();
            foreach (var alias in Request.Aliases)
            {
                unit.Aliases.Add(new IngredientUnitAlias { Id = Guid.NewGuid(), Name = alias, UnitId = unit.Id });
            }
        }

        unit.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return UnitMappings.MapToResponse(unit);
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
