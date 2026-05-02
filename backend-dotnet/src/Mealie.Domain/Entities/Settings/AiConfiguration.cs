namespace Mealie.Domain.Entities.Settings;

/// <summary>
///     Admin-configured external AI service that can be used for ingredient parsing
///     and other AI-assisted features (image import, transcription).
///     Maps to the <c>ai_configurations</c> table.
/// </summary>
public class AiConfiguration
{
    public Guid Id { get; set; }

    /// <summary>Human-readable label, e.g. "Company OpenAI".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>One of: openAi, azureOpenAi, anthropic, ollama, custom.</summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>AES-encrypted API key; null for providers that don't use one (e.g. local Ollama).</summary>
    public string? EncryptedApiKey { get; set; }

    /// <summary>Base URL; required for azureOpenAi, ollama, custom; optional for others.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Model ID string, e.g. "gpt-4o"; falls back to provider SDK default if null.</summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    ///     Denormalized from <c>site_settings.default_parser</c>; at most one row is <c>true</c>.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>Gates image-import feature for this provider.</summary>
    public bool EnableImageServices { get; set; } = true;

    /// <summary>Gates audio-transcription feature for this provider.</summary>
    public bool EnableTranscriptionServices { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
