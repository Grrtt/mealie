using Mealie.Application.Contracts.Search;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Parser;

public class FoodMatcher(IFoodSearchIndex foodIndex, ApplicationDbContext db)
{
    public async Task<(IngredientFood? Food, string Note)> MatchAsync(Guid groupId, string text,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, text);
        }

        var result = await foodIndex.SearchAsync(new FoodSearchQuery(groupId, text), ct);
        if (result is null)
        {
            return (null, text);
        }

        var food = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == result.FoodId, ct);
        return (food, result.Remainder);
    }
}
