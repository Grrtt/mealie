namespace Mealie.Application.Dtos.Admin;

// ── Response ─────────────────────────────────────────────────────────────────

/// <summary>AI provider configuration returned in API responses. Never includes a plaintext API key.</summary>
public class AiConfigurationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>True if an API key is stored (encrypted) for this provider.</summary>
    public bool HasApiKey { get; set; }

    /// <summary>Masked key preview, e.g. "sk-...••••1234". Never the real key.</summary>
    public string? MaskedApiKey { get; set; }

    public string? BaseUrl { get; set; }
    public string? DefaultModel { get; set; }
    public bool IsActive { get; set; }
    public bool EnableImageServices { get; set; }
    public bool EnableTranscriptionServices { get; set; }
    public DateTime? CreatedAt { get; set; }
}

// ── Create ────────────────────────────────────────────────────────────────────

public class CreateAiConfigurationRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>One of: openAi, azureOpenAi, anthropic, ollama, custom.</summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>Plaintext API key; encrypted before storage. Nullable for keyless providers.</summary>
    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }
    public string? DefaultModel { get; set; }
    public bool EnableImageServices { get; set; } = true;
    public bool EnableTranscriptionServices { get; set; } = true;
}

// ── Update ────────────────────────────────────────────────────────────────────

public class UpdateAiConfigurationRequest
{
    /// <summary>Null = no change to name.</summary>
    public string? Name { get; set; }

    /// <summary>
    ///     Null = keep existing key.
    ///     Empty string = clear the key.
    ///     Non-empty string = replace with new encrypted value.
    /// </summary>
    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }
    public string? DefaultModel { get; set; }
    public bool? EnableImageServices { get; set; }
    public bool? EnableTranscriptionServices { get; set; }
}
