using System.Text.RegularExpressions;
using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Admin;

public record DeleteAdminHouseholdCommand(Guid HouseholdId) : IQuery<bool>
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