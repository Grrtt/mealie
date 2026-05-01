using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Webhooks;

public record DeleteWebhookCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var webhook = await db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == HouseholdId && w.Id == Id, ct);
        if (webhook is null)
        {
            return false;
        }

        db.Webhooks.Remove(webhook);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
