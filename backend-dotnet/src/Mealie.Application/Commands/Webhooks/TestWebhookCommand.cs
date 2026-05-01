using Mealie.Application.Dtos.Webhooks;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Webhooks;

public record TestWebhookCommand(string Url) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        await services.WebhookDeliveryService.DeliverAsync(Url, new { event_type = "test", timestamp = DateTime.UtcNow });
        return true;
    }
}