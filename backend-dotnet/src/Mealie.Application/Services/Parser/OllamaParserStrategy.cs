using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using a locally-running Ollama instance.
/// Ollama exposes an OpenAI-compatible chat completions endpoint.
/// </summary>
public class OllamaParserStrategy(
    AiParserConfig config,
    IHttpClientFactory httpClientFactory,
    ILogger<OllamaParserStrategy> logger)
    : OpenAiCompatibleParserStrategy(config, httpClientFactory, logger)
{
    protected override HttpClient BuildClient()
    {
        var client = httpClientFactory.CreateClient();
        if (!string.IsNullOrEmpty(config.BaseUrl))
            client.BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/");
        // Ollama doesn't require auth by default; send key if provided
        if (!string.IsNullOrEmpty(config.ApiKey))
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
        return client;
    }
}
