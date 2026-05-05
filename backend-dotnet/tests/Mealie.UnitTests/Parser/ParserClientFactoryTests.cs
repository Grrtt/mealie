using System.Net.Http.Headers;
using Mealie.Application.Services.Parser;

namespace Mealie.UnitTests.Parser;

public class ParserClientFactoryTests
{
    [Fact]
    public void CreateClient_ConfiguresOpenAiNamedClient()
    {
        var openAiClient = new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/") };
        var factory = new ParserClientFactory(new RecordingHttpClientFactory(openAiClient, ["OpenAi"]));

        var client = factory.CreateClient(
            new AiParserConfig("secret", null, "gpt-4o-mini", "project-123"),
            ParserProviderDescriptor.OpenAi);

        Assert.Equal("OpenAi", RecordingHttpClientFactory.LastRequestedName);
        Assert.Equal(new Uri("https://api.openai.com/v1/"), client.BaseAddress);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal("secret", client.DefaultRequestHeaders.Authorization?.Parameter);
        Assert.Equal("project-123", client.DefaultRequestHeaders.GetValues("OpenAI-Project").Single());
    }

    [Fact]
    public void CreateClient_ConfiguresAzureHeadersAndBaseAddress()
    {
        var factory = new ParserClientFactory(new RecordingHttpClientFactory(new HttpClient()));

        var client = factory.CreateClient(
            new AiParserConfig("azure-key", "https://example.openai.azure.com/openai/deployments/test", "gpt-4o-mini", null),
            ParserProviderDescriptor.AzureOpenAi);

        Assert.Equal(new Uri("https://example.openai.azure.com/openai/deployments/test/"), client.BaseAddress);
        Assert.Equal("azure-key", client.DefaultRequestHeaders.GetValues("api-key").Single());
        Assert.Null(client.DefaultRequestHeaders.Authorization);
    }

    [Fact]
    public void CreateClient_ConfiguresOllamaBaseAddressAndBearerWhenApiKeyProvided()
    {
        var factory = new ParserClientFactory(new RecordingHttpClientFactory(new HttpClient()));

        var client = factory.CreateClient(
            new AiParserConfig("ollama-key", "http://localhost:11434/v1", "llama3.1", null),
            ParserProviderDescriptor.Ollama);

        Assert.Equal(new Uri("http://localhost:11434/v1/"), client.BaseAddress);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal("ollama-key", client.DefaultRequestHeaders.Authorization?.Parameter);
    }

    [Fact]
    public void CreateClient_ConfiguresCustomProvider()
    {
        var factory = new ParserClientFactory(new RecordingHttpClientFactory(new HttpClient()));

        var client = factory.CreateClient(
            new AiParserConfig("custom-key", "https://custom.example/api", "custom-model", null),
            ParserProviderDescriptor.Custom);

        Assert.Equal(new Uri("https://custom.example/api/"), client.BaseAddress);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "custom-key"), client.DefaultRequestHeaders.Authorization);
    }

    private sealed class RecordingHttpClientFactory(HttpClient defaultClient, params IEnumerable<string>[] namedClients)
        : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClient> _namedClients = namedClients
            .SelectMany(names => names)
            .Distinct()
            .ToDictionary(name => name, _ => defaultClient);

        public static string? LastRequestedName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            LastRequestedName = name;
            return _namedClients.TryGetValue(name, out var client) ? client : defaultClient;
        }
    }
}
