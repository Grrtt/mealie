using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Webhooks;

public record RerunWebhooksForHouseholdCommand(Guid HouseholdId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var webhooks = await db.Webhooks.IgnoreQueryFilters()
            .Where(w => w.HouseholdId == HouseholdId && w.Enabled).ToListAsync(ct);
        var plans = await db.MealPlans.IgnoreQueryFilters()
            .Where(mp => mp.HouseholdId == HouseholdId && mp.Date == today).ToListAsync(ct);

        foreach (var webhook in webhooks)
        {
            var payload = new
            {
                event_type = "meal_plan", date = today.ToString("yyyy-MM-dd"),
                household_id = HouseholdId, plan_count = plans.Count
            };
            await services.WebhookDeliveryService.DeliverAsync(webhook.Url, payload);
        }

        return true;
    }
}
