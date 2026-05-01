using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record DeleteUnitCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var unit = await db.Units.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GroupId == GroupId && u.Id == Id, ct);
        if (unit is null)
        {
            return false;
        }

        db.Units.Remove(unit);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
