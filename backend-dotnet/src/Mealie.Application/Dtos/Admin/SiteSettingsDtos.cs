namespace Mealie.Application.Dtos.Admin;

/// <summary>Site-wide settings returned in API responses.</summary>
public class SiteSettingsResponse
{
    /// <summary>"nlp" | "brute" | &lt;ai_configuration_uuid&gt;</summary>
    public string DefaultParser { get; set; } = "nlp";

    /// <summary>True if the default_parser points to a deleted or missing AI configuration.</summary>
    public bool DefaultParserUnavailable { get; set; }

    /// <summary>Custom system prompt for ingredient parsing. Null = use built-in default.</summary>
    public string? IngredientSystemPrompt { get; set; }

    /// <summary>Custom system prompt for AI category assignment. Null = use built-in default.</summary>
    public string? CategorySystemPrompt { get; set; }

    /// <summary>Custom system prompt for AI tag assignment. Null = use built-in default.</summary>
    public string? TagSystemPrompt { get; set; }

    /// <summary>Built-in default ingredient system prompt shown when no custom prompt is set.</summary>
    public string DefaultIngredientSystemPrompt { get; set; } = string.Empty;

    /// <summary>Built-in default category system prompt shown when no custom prompt is set.</summary>
    public string DefaultCategorySystemPrompt { get; set; } = string.Empty;

    /// <summary>Built-in default tag system prompt shown when no custom prompt is set.</summary>
    public string DefaultTagSystemPrompt { get; set; } = string.Empty;
}

public class UpdateSiteSettingsRequest
{
    /// <summary>"nlp" | "brute" | &lt;ai_configuration_uuid&gt;</summary>
    public string DefaultParser { get; set; } = "nlp";

    /// <summary>Null = no change. Empty string = clear (revert to built-in default).</summary>
    public string? IngredientSystemPrompt { get; set; }
    public string? CategorySystemPrompt { get; set; }
    public string? TagSystemPrompt { get; set; }
}
