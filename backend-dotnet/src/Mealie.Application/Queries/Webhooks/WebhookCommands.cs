using Mealie.Application.Dtos.Webhooks;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Webhooks;

public record GetWebhooksQuery(Guid HouseholdId) : IQuery<IList<WebhookResponse>>
{
    public async Task<IList<WebhookResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var webhooks = await services.Db.Webhooks.IgnoreQueryFilters()
            .Where(w => w.HouseholdId == HouseholdId).ToListAsync(ct);
        return webhooks.Select(WebhookMappings.MapToResponse).ToList();
    }
}

public record GetWebhookByIdQuery(Guid HouseholdId, Guid Id) : IQuery<WebhookResponse?>
{
    public async Task<WebhookResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var webhook = await services.Db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == HouseholdId && w.Id == Id, ct);
        return webhook is null ? null : WebhookMappings.MapToResponse(webhook);
    }
}

public record CreateWebhookCommand(Guid GroupId, Guid HouseholdId, CreateWebhookRequest Request) : IQuery<WebhookResponse>
{
    public async Task<WebhookResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var webhook = new Webhook
        {
            Id = Guid.NewGuid(), Name = Request.Name, Url = Request.Url,
            Enabled = Request.Enabled, ScheduledTime = Request.ScheduledTime,
            GroupId = GroupId, HouseholdId = HouseholdId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Webhooks.Add(webhook);
        await db.SaveChangesAsync(ct);
        return WebhookMappings.MapToResponse(webhook);
    }
}

public record UpdateWebhookCommand(Guid HouseholdId, Guid Id, CreateWebhookRequest Request) : IQuery<WebhookResponse?>
{
    public async Task<WebhookResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var webhook = await db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == HouseholdId && w.Id == Id, ct);
        if (webhook is null) return null;
        webhook.Name = Request.Name;
        webhook.Url = Request.Url;
        webhook.Enabled = Request.Enabled;
        webhook.ScheduledTime = Request.ScheduledTime;
        webhook.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return WebhookMappings.MapToResponse(webhook);
    }
}

public record DeleteWebhookCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var webhook = await db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == HouseholdId && w.Id == Id, ct);
        if (webhook is null) return false;
        db.Webhooks.Remove(webhook);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record TestWebhookCommand(string Url) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        await services.WebhookDeliveryService.DeliverAsync(Url, new { event_type = "test", timestamp = DateTime.UtcNow });
        return true;
    }
}

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

file static class WebhookMappings
{
    public static WebhookResponse MapToResponse(Webhook w) =>
        new() { Id = w.Id, Name = w.Name, Url = w.Url, Enabled = w.Enabled, ScheduledTime = w.ScheduledTime };
}
