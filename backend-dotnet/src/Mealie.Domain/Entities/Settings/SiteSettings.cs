namespace Mealie.Domain.Entities.Settings;

/// <summary>
///     Singleton server-global settings table. Exactly one row exists (seeded by migration;
///     created lazily by <c>GetOrCreateAsync</c> if missing).
///     Maps to the <c>site_settings</c> table.
/// </summary>
public class SiteSettings
{
    public Guid Id { get; set; }

    /// <summary>
    ///     "nlp" | "brute" | &lt;ai_configuration.id&gt;.
    ///     Defaults to "nlp" when not set or when the referenced AI provider is deleted.
    /// </summary>
    public string DefaultParser { get; set; } = "nlp";

    /// <summary>Custom system prompt for ingredient parsing. Null = use built-in default.</summary>
    public string? IngredientSystemPrompt { get; set; }

    /// <summary>Custom system prompt for AI category assignment. Null = use built-in default.</summary>
    public string? CategorySystemPrompt { get; set; }

    /// <summary>Custom system prompt for AI tag assignment. Null = use built-in default.</summary>
    public string? TagSystemPrompt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
