using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

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