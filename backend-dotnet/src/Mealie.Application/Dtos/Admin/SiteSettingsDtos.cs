namespace Mealie.Application.Dtos.Admin;

/// <summary>Site-wide settings returned in API responses.</summary>
public class SiteSettingsResponse
{
    /// <summary>"nlp" | "brute" | &lt;ai_configuration_uuid&gt;</summary>
    public string DefaultParser { get; set; } = "nlp";

    /// <summary>True if the default_parser points to a deleted or missing AI configuration.</summary>
    public bool DefaultParserUnavailable { get; set; }

    /// <summary>True if the OPENAI_API_KEY environment variable is still set at runtime.</summary>
    public bool LegacyEnvVarsDetected { get; set; }
}

public class UpdateSiteSettingsRequest
{
    /// <summary>"nlp" | "brute" | &lt;ai_configuration_uuid&gt;</summary>
    public string DefaultParser { get; set; } = "nlp";
}
