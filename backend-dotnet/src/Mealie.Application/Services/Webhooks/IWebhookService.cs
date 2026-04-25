using Mealie.Application.Dtos.Webhooks;

namespace Mealie.Application.Services.Webhooks;

public interface IWebhookService
{
    Task<IList<WebhookResponse>> GetAllAsync(Guid householdId, CancellationToken ct = default);
    Task<WebhookResponse> CreateAsync(Guid groupId, Guid householdId, CreateWebhookRequest request, CancellationToken ct = default);
    Task<WebhookResponse?> UpdateAsync(Guid householdId, Guid id, CreateWebhookRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default);
    Task TestAsync(string url, CancellationToken ct = default);
}
