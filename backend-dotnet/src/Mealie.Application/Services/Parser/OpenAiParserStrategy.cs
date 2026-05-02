using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using the OpenAI chat completions API.
/// Supports project-scoped API keys via the OpenAI-Project header.
/// </summary>
public class OpenAiParserStrategy(
    AiParserConfig config,
    IHttpClientFactory httpClientFactory,
    ILogger<OpenAiParserStrategy> logger)
    : OpenAiCompatibleParserStrategy(config, httpClientFactory, logger)
{
    protected override HttpClient BuildClient()
    {
        var client = httpClientFactory.CreateClient("OpenAi");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);

        if (!string.IsNullOrEmpty(config.ProjectId))
        {
            client.DefaultRequestHeaders.Remove("OpenAI-Project");
            client.DefaultRequestHeaders.Add("OpenAI-Project", config.ProjectId);
        }

        return client;
    }
}
