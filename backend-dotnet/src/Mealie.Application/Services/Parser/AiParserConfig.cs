namespace Mealie.Application.Services.Parser;

/// <summary>
/// Configuration bag passed to an AI parser strategy at resolution time.
/// Credentials are already decrypted; do not log or persist.
/// </summary>
public record AiParserConfig(
    string ApiKey,
    string? BaseUrl,
    string Model,
    string? ProjectId);
