using Mealie.Application.Queries;

namespace Mealie.Application.Commands.Webhooks;

public record TestWebhookCommand(string Url) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        await services.WebhookDeliveryService.DeliverAsync(Url,
            new { event_type = "test", timestamp = DateTime.UtcNow });
        return true;
    }
}
