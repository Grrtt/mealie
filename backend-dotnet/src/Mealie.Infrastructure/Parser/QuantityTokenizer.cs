using System.Text.RegularExpressions;

namespace Mealie.Infrastructure.Parser;

public static partial class QuantityTokenizer
{
    // Unicode vulgar fractions
    private static readonly Dictionary<char, decimal> VulgarFractions = new()
    {
        ['½'] = 0.5m, ['⅓'] = 1m / 3, ['⅔'] = 2m / 3, ['¼'] = 0.25m,
        ['¾'] = 0.75m, ['⅛'] = 0.125m, ['⅜'] = 0.375m, ['⅝'] = 0.625m, ['⅞'] = 0.875m
    };

    [GeneratedRegex(@"^(\d+)\s+(\d+)/(\d+)")]
    private static partial Regex MixedFractionRegex();

    [GeneratedRegex(@"^(\d+)/(\d+)")]
    private static partial Regex SimpleFractionRegex();

    [GeneratedRegex(@"^(\d+(?:\.\d+)?)\s*[-–]\s*(\d+(?:\.\d+)?)")]
    private static partial Regex RangeRegex();

    [GeneratedRegex(@"^(\d+(?:\.\d+)?)")]
    private static partial Regex DecimalRegex();

    public static (decimal? Quantity, string Remainder) Tokenize(string input)
    {
        var text = input.Trim();

        // Unicode vulgar fractions
        if (text.Length > 0 && VulgarFractions.TryGetValue(text[0], out var vulgar))
        {
            return (vulgar, text[1..].Trim());
        }

        // Range (take average)
        var rangeMatch = RangeRegex().Match(text);
        if (rangeMatch.Success)
        {
            var qty = (decimal.Parse(rangeMatch.Groups[1].Value) + decimal.Parse(rangeMatch.Groups[2].Value)) / 2;
            return (qty, text[rangeMatch.Length..].Trim());
        }

        // Mixed fraction: "1 1/2"
        var mixedMatch = MixedFractionRegex().Match(text);
        if (mixedMatch.Success)
        {
            var whole = decimal.Parse(mixedMatch.Groups[1].Value);
            var num = decimal.Parse(mixedMatch.Groups[2].Value);
            var den = decimal.Parse(mixedMatch.Groups[3].Value);
            return (whole + num / den, text[mixedMatch.Length..].Trim());
        }

        // Simple fraction: "1/2"
        var simpleFracMatch = SimpleFractionRegex().Match(text);
        if (simpleFracMatch.Success)
        {
            var num = decimal.Parse(simpleFracMatch.Groups[1].Value);
            var den = decimal.Parse(simpleFracMatch.Groups[2].Value);
            return (num / den, text[simpleFracMatch.Length..].Trim());
        }

        // Decimal/integer
        var decimalMatch = DecimalRegex().Match(text);
        if (decimalMatch.Success)
        {
            // Check for Unicode vulgar fraction following number
            var remainder = text[decimalMatch.Length..].Trim();
            var baseQty = decimal.Parse(decimalMatch.Groups[1].Value);
            if (remainder.Length > 0 && VulgarFractions.TryGetValue(remainder[0], out var fracPart))
            {
                return (baseQty + fracPart, remainder[1..].Trim());
            }

            return (baseQty, remainder);
        }

        return (null, text);
    }
}
