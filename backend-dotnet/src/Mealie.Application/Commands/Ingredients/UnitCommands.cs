using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

using Mealie.Application.Queries;
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

public record UpdateUnitCommand(Guid GroupId, Guid Id, UpdateUnitRequest Request) : IQuery<UnitResponse?>
{
    public async Task<UnitResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var unit = await db.Units.IgnoreQueryFilters()
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.GroupId == GroupId && u.Id == Id, ct);
        if (unit is null) return null;

        if (Request.Name is not null) unit.Name = Request.Name;
        if (Request.Description is not null) unit.Description = Request.Description;
        if (Request.Abbreviation is not null) unit.Abbreviation = Request.Abbreviation;
        if (Request.PluralName is not null) unit.PluralName = Request.PluralName;
        if (Request.PluralAbbreviation is not null) unit.PluralAbbreviation = Request.PluralAbbreviation;
        if (Request.UseAbbreviation.HasValue) unit.UseAbbreviation = Request.UseAbbreviation.Value;
        if (Request.Fraction.HasValue) unit.Fraction = Request.Fraction.Value;

        if (Request.Aliases is not null)
        {
            unit.Aliases.Clear();
            foreach (var alias in Request.Aliases)
                unit.Aliases.Add(new IngredientUnitAlias { Id = Guid.NewGuid(), Name = alias, UnitId = unit.Id });
        }

        unit.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return UnitMappings.MapToResponse(unit);
    }
}

public record DeleteUnitCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var unit = await db.Units.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GroupId == GroupId && u.Id == Id, ct);
        if (unit is null) return false;
        db.Units.Remove(unit);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record MergeUnitCommand(Guid GroupId, Guid FromUnitId, Guid ToUnitId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var fromUnit = await db.Units.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GroupId == GroupId && u.Id == FromUnitId, ct);
        var toUnit = await db.Units.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GroupId == GroupId && u.Id == ToUnitId, ct);
        if (fromUnit is null || toUnit is null) return false;

        await db.RecipeIngredients.Where(i => i.UnitId == FromUnitId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UnitId, ToUnitId), ct);
        db.Units.Remove(fromUnit);
        await db.SaveChangesAsync(ct);
        return true;
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
