using Mealie.Application.Dtos.Webhooks;
using Mealie.Domain.Entities.Settings;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Webhooks;

public class WebhookService(ApplicationDbContext db, IWebhookDeliveryService deliveryService) : IWebhookService
{
    public async Task<IList<WebhookResponse>> GetAllAsync(Guid householdId, CancellationToken ct = default)
    {
        var webhooks = await db.Webhooks.IgnoreQueryFilters()
            .Where(w => w.HouseholdId == householdId)
            .ToListAsync(ct);
        return webhooks.Select(MapToResponse).ToList();
    }

    public async Task<WebhookResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var webhook = await db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == householdId && w.Id == id, ct);
        return webhook is null ? null : MapToResponse(webhook);
    }

    public async Task<WebhookResponse> CreateAsync(Guid groupId, Guid householdId, CreateWebhookRequest request,
        CancellationToken ct = default)
    {
        var webhook = new Webhook
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Url = request.Url,
            Enabled = request.Enabled,
            ScheduledTime = request.ScheduledTime,
            GroupId = groupId,
            HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.Webhooks.Add(webhook);
        await db.SaveChangesAsync(ct);
        return MapToResponse(webhook);
    }

    public async Task<WebhookResponse?> UpdateAsync(Guid householdId, Guid id, CreateWebhookRequest request,
        CancellationToken ct = default)
    {
        var webhook = await db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == householdId && w.Id == id, ct);
        if (webhook is null)
        {
            return null;
        }

        webhook.Name = request.Name;
        webhook.Url = request.Url;
        webhook.Enabled = request.Enabled;
        webhook.ScheduledTime = request.ScheduledTime;
        webhook.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(webhook);
    }

    public async Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var webhook = await db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == householdId && w.Id == id, ct);
        if (webhook is null)
        {
            return false;
        }

        db.Webhooks.Remove(webhook);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task TestAsync(string url, CancellationToken ct = default)
    {
        return deliveryService.DeliverAsync(url, new { event_type = "test", timestamp = DateTime.UtcNow });
    }

    public async Task RerunForHouseholdAsync(Guid householdId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var webhooks = await db.Webhooks.IgnoreQueryFilters()
            .Where(w => w.HouseholdId == householdId && w.Enabled)
            .ToListAsync(ct);

        var plans = await db.MealPlans.IgnoreQueryFilters()
            .Where(mp => mp.HouseholdId == householdId && mp.Date == today)
            .ToListAsync(ct);

        foreach (var webhook in webhooks)
        {
            var payload = new
            {
                event_type = "meal_plan", date = today.ToString("yyyy-MM-dd"), household_id = householdId,
                plan_count = plans.Count
            };
            await deliveryService.DeliverAsync(webhook.Url, payload);
        }
    }

    private static WebhookResponse MapToResponse(Webhook w)
    {
        return new WebhookResponse
        {
            Id = w.Id,
            Name = w.Name,
            Url = w.Url,
            Enabled = w.Enabled,
            ScheduledTime = w.ScheduledTime
        };
    }
}
