using Mealie.Application.Common;
using Mealie.Application.Dtos.Groups;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Groups;

public record DeleteHouseholdCommand(Guid HouseholdId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        if (h is null) return false;
        db.Households.Remove(h);
        await db.SaveChangesAsync(ct);
        return true;
    }
}