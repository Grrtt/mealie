using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Commands.Webhooks;

public record DeleteEventNotifierCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var notifier = await db.EventNotifiers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.HouseholdId == HouseholdId && e.Id == Id, ct);
        if (notifier is null) return false;
        db.EventNotifiers.Remove(notifier);
        await db.SaveChangesAsync(ct);
        return true;
    }
}