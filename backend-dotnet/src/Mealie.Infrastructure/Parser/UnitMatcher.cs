using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Infrastructure.Parser;

public class UnitMatcher(ApplicationDbContext db)
{
    public async Task<(IngredientUnit? Unit, string Remainder)> MatchAsync(Guid groupId, string text,
        CancellationToken ct = default)
    {
        var units = await db.Units.IgnoreQueryFilters()
            .Where(u => u.GroupId == groupId)
            .ToListAsync(ct);

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return (null, text);
        }

        // Try matching longest-first (e.g., "fluid oz" before "oz")
        for (var len = Math.Min(words.Length, 3); len >= 1; len--)
        {
            var candidate = string.Join(' ', words.Take(len)).ToLower();
            var unit = units.FirstOrDefault(u =>
                Normalize(u.Name) == candidate ||
                Normalize(u.PluralName) == candidate ||
                Normalize(u.Abbreviation) == candidate ||
                Normalize(u.PluralAbbreviation) == candidate);

            if (unit is not null)
            {
                var remainder = string.Join(' ', words.Skip(len)).Trim();
                return (unit, remainder);
            }
        }

        return (null, text);
    }

    private static string Normalize(string? s)
    {
        return (s ?? "").Trim().ToLower();
    }
}
