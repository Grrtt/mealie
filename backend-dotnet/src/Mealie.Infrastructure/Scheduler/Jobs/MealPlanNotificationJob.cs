using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Infrastructure.Scheduler.Jobs;

public class MealPlanNotificationJob(
    ApplicationDbContext db,
    IWebhookDeliveryService webhookService,
    ILogger<MealPlanNotificationJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        logger.LogInformation("Running meal plan notification job for {Date}", today);

        var plans = await db.MealPlans.IgnoreQueryFilters()
            .Where(mp => mp.Date == today)
            .ToListAsync(ct);

        logger.LogInformation("Found {Count} meal plans for today", plans.Count);

        var groupIds = plans.Select(p => p.GroupId).Distinct().ToList();
        foreach (var groupId in groupIds)
        {
            var webhooks = await db.Webhooks.IgnoreQueryFilters()
                .Where(w => w.GroupId == groupId && w.Enabled && w.ScheduledTime != null)
                .ToListAsync(ct);

            foreach (var webhook in webhooks)
            {
                var payload = new { event_type = "meal_plan", date = today.ToString("yyyy-MM-dd"), group_id = groupId };
                await webhookService.DeliverAsync(webhook.Url, payload);
            }
        }
    }
}
