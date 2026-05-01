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

file static class WebhookMappings
{
    public static WebhookResponse MapToResponse(Webhook w) =>
        new() { Id = w.Id, Name = w.Name, Url = w.Url, Enabled = w.Enabled, ScheduledTime = w.ScheduledTime };
}
