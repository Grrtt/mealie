using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Webhooks;

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

file static class WebhookMappings
{
    public static WebhookResponse MapToResponse(Webhook w) =>
        new() { Id = w.Id, Name = w.Name, Url = w.Url, Enabled = w.Enabled, ScheduledTime = w.ScheduledTime };
}