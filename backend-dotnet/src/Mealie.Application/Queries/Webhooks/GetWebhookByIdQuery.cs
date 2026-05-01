using Mealie.Application.Dtos.Webhooks;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Webhooks;

public record GetWebhookByIdQuery(Guid HouseholdId, Guid Id) : IQuery<WebhookResponse?>
{
    public async Task<WebhookResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var webhook = await services.Db.Webhooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.HouseholdId == HouseholdId && w.Id == Id, ct);
        return webhook is null ? null : WebhookMappings.MapToResponse(webhook);
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
