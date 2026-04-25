using System.Text.RegularExpressions;

namespace Mealie.Application.Common;

public static partial class SlugHelper
{
    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();

    public static string Generate(string name)
    {
        var lower = name.ToLowerInvariant();
        var slug = NonAlphanumeric().Replace(lower, "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "recipe" : slug;
    }
}
