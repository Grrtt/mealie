using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using a custom OpenAI-compatible endpoint.
/// </summary>
public class CustomAiParserStrategy(
    AiParserConfig config,
    IHttpClientFactory httpClientFactory,
    ILogger<CustomAiParserStrategy> logger)
    : OpenAiCompatibleParserStrategy(config, httpClientFactory, logger)
{
    protected override HttpClient BuildClient()
    {
        var client = httpClientFactory.CreateClient();
        if (!string.IsNullOrEmpty(config.BaseUrl))
            client.BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
        return client;
    }
}
