using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Mealie.Infrastructure.Webhooks;

public interface IWebhookDeliveryService
{
    Task DeliverAsync(string url, object payload);
}

public class WebhookDeliveryService(HttpClient httpClient, ILogger<WebhookDeliveryService> logger)
    : IWebhookDeliveryService
{
    public async Task DeliverAsync(string url, object payload)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Webhook delivery failed. Url={Url} StatusCode={StatusCode}", url,
                    (int)response.StatusCode);
            }
            else
            {
                logger.LogInformation("Webhook delivered to {Url}", url);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook delivery exception. Url={Url} Exception={Exception}", url, ex.Message);
        }
    }
}
