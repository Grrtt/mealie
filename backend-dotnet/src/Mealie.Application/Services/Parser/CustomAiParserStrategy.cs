using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using a custom OpenAI-compatible endpoint.
/// </summary>
public class CustomAiParserStrategy(
    AiParserConfig config,
    ParserClientFactory clientFactory,
    ILogger<CustomAiParserStrategy> logger)
    : OpenAiCompatibleParserStrategy(config, clientFactory, ParserProviderDescriptor.Custom, logger);
