using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Webhooks;

public record UpdateWebhookCommand(Guid HouseholdId, Guid Id, CreateWebhookRequest Request) : IQuery<WebhookResponse?>
{
    public async Task<WebhookResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var webhook = await db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == HouseholdId && w.Id == Id, ct);
        if (webhook is null)
        {
            return null;
        }

        webhook.Name = Request.Name;
        webhook.Url = Request.Url;
        webhook.Enabled = Request.Enabled;
        webhook.ScheduledTime = Request.ScheduledTime;
        webhook.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return WebhookMappings.MapToResponse(webhook);
    }
}

file static class WebhookMappings
{
    public static WebhookResponse MapToResponse(Webhook w)
    {
        return new WebhookResponse
            { Id = w.Id, Name = w.Name, Url = w.Url, Enabled = w.Enabled, ScheduledTime = w.ScheduledTime };
    }
}
