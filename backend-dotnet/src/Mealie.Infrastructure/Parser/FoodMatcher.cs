using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Infrastructure.Parser;

public class FoodMatcher(ApplicationDbContext db)
{
    public async Task<(IngredientFood? Food, string Note)> MatchAsync(Guid groupId, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return (null, text);

        var foods = await db.Foods.IgnoreQueryFilters()
            .Where(f => f.GroupId == groupId)
            .Include(f => f.Aliases)
            .ToListAsync(ct);

        var normalized = text.Trim().ToLower();

        // Try longest name match first
        foreach (var food in foods.OrderByDescending(f => f.Name?.Length ?? 0))
        {
            var foodName = (food.Name ?? "").ToLower();
            if (!string.IsNullOrEmpty(foodName) && normalized.StartsWith(foodName))
            {
                var note = text[foodName.Length..].Trim().Trim(',').Trim();
                return (food, note);
            }

            // Check aliases
            var matchingAlias = food.Aliases
                .Where(a => !string.IsNullOrEmpty(a.Name))
                .OrderByDescending(a => a.Name!.Length)
                .FirstOrDefault(a => normalized.StartsWith(a.Name!.ToLower()));

            if (matchingAlias is not null)
            {
                var note = text[(matchingAlias.Name?.Length ?? 0)..].Trim().Trim(',').Trim();
                return (food, note);
            }
        }

        // No match — entire text is the note/food name
        return (null, text);
    }
}
