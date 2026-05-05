using System.Net.Http.Headers;

namespace Mealie.Application.Services.Parser;

public class ParserClientFactory(IHttpClientFactory httpClientFactory)
{
    public HttpClient CreateClient(AiParserConfig config, ParserProviderDescriptor descriptor)
    {
        var client = descriptor.NamedClient is null
            ? httpClientFactory.CreateClient()
            : httpClientFactory.CreateClient(descriptor.NamedClient);

        if (descriptor.UseBaseUrl && !string.IsNullOrWhiteSpace(config.BaseUrl))
        {
            client.BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/");
        }

        if (descriptor.UseBearerAuth && !string.IsNullOrWhiteSpace(config.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        }

        if (descriptor.UseApiKeyHeader && !string.IsNullOrWhiteSpace(config.ApiKey))
        {
            client.DefaultRequestHeaders.Remove("api-key");
            client.DefaultRequestHeaders.Add("api-key", config.ApiKey);
        }

        if (descriptor.UseProjectHeader)
        {
            client.DefaultRequestHeaders.Remove("OpenAI-Project");
            if (!string.IsNullOrWhiteSpace(config.ProjectId))
            {
                client.DefaultRequestHeaders.Add("OpenAI-Project", config.ProjectId);
            }
        }

        return client;
    }
}
