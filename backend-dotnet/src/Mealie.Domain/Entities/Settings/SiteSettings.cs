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

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
