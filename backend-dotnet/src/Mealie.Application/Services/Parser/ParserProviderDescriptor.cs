namespace Mealie.Application.Services.Parser;

public sealed record ParserProviderDescriptor(
    string ProviderType,
    string? NamedClient = null,
    bool UseBaseUrl = false,
    bool UseBearerAuth = false,
    bool UseApiKeyHeader = false,
    bool UseProjectHeader = false)
{
    public static ParserProviderDescriptor OpenAi { get; } =
        new("openAi", NamedClient: "OpenAi", UseBearerAuth: true, UseProjectHeader: true);

    public static ParserProviderDescriptor AzureOpenAi { get; } =
        new("azureOpenAi", UseBaseUrl: true, UseApiKeyHeader: true);

    public static ParserProviderDescriptor Ollama { get; } =
        new("ollama", UseBaseUrl: true, UseBearerAuth: true);

    public static ParserProviderDescriptor Custom { get; } =
        new("custom", UseBaseUrl: true, UseBearerAuth: true);
}
