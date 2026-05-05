using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using a locally-running Ollama instance.
/// Ollama exposes an OpenAI-compatible chat completions endpoint.
/// </summary>
public class OllamaParserStrategy(
    AiParserConfig config,
    ParserClientFactory clientFactory,
    ILogger<OllamaParserStrategy> logger)
    : OpenAiCompatibleParserStrategy(config, clientFactory, ParserProviderDescriptor.Ollama, logger);
