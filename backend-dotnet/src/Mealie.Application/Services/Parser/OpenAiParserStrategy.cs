using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using the OpenAI chat completions API.
/// Supports project-scoped API keys via the OpenAI-Project header.
/// </summary>
public class OpenAiParserStrategy(
    AiParserConfig config,
    ParserClientFactory clientFactory,
    ILogger<OpenAiParserStrategy> logger)
    : OpenAiCompatibleParserStrategy(config, clientFactory, ParserProviderDescriptor.OpenAi, logger);
