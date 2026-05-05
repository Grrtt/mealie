using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using Azure OpenAI Service.
/// Requires BaseUrl (e.g. https://&lt;resource&gt;.openai.azure.com/openai/deployments/&lt;deployment&gt;/).
/// The API key is sent via api-key header instead of Bearer.
/// </summary>
public class AzureOpenAiParserStrategy(
    AiParserConfig config,
    ParserClientFactory clientFactory,
    ILogger<AzureOpenAiParserStrategy> logger)
    : OpenAiCompatibleParserStrategy(config, clientFactory, ParserProviderDescriptor.AzureOpenAi, logger);
