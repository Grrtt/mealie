using System.Text.RegularExpressions;

namespace Mealie.Infrastructure.Parser;

/// <summary>
///     Strips non-semantic decoration (checkboxes, bullets, excess whitespace) from
///     raw ingredient strings before they enter the quantity/unit/food pipeline.
/// </summary>
public static partial class IngredientNormalizer
{
    // Checkbox glyphs commonly injected by recipe sites (▢ □ ☐ ☑ ☒ ◻ ◽ ▪ ▫ •)
    [GeneratedRegex(
        @"^[\u25A1\u25A2\u2610\u2611\u2612\u25FB\u25FC\u25FD\u25FE\u25AA\u25AB\u2022\u2023\u2043\u204C\u204D\u2219\u2713\u2714\u2715\u2716]+\s*")]
    private static partial Regex LeadingCheckboxRegex();

    // Collapse internal runs of whitespace to a single space
    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultiSpaceRegex();

    public static string Normalize(string input)
    {
        var text = input.Trim();
        text = LeadingCheckboxRegex().Replace(text, string.Empty);
        text = MultiSpaceRegex().Replace(text, " ");
        return text.Trim();
    }
}
