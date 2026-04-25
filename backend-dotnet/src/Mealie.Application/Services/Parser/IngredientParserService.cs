using Mealie.Infrastructure.Parser;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

public class IngredientParserService(
    UnitMatcher unitMatcher,
    FoodMatcher foodMatcher,
    ILogger<IngredientParserService> logger) : IIngredientParserService
{
    public async Task<ParsedIngredientDto> ParseAsync(Guid groupId, string ingredientString, CancellationToken ct = default)
    {
        var (quantity, afterQuantity) = QuantityTokenizer.Tokenize(ingredientString);
        var (unit, afterUnit) = await unitMatcher.MatchAsync(groupId, afterQuantity, ct);
        var (food, note) = await foodMatcher.MatchAsync(groupId, afterUnit, ct);

        return new ParsedIngredientDto
        {
            Input = ingredientString,
            Quantity = quantity,
            Unit = unit is not null ? new ParsedIngredientUnitDto { Id = unit.Id, Name = unit.Name } : null,
            Food = food is not null ? new ParsedIngredientFoodDto { Id = food.Id, Name = food.Name } : null,
            Note = string.IsNullOrEmpty(note) ? null : note,
            Confidence = quantity.HasValue && (unit is not null || food is not null) ? "high" : "low"
        };
    }

    public async Task<IList<ParsedIngredientDto>> ParseBatchAsync(Guid groupId, IList<string> ingredients, CancellationToken ct = default)
    {
        var results = new List<ParsedIngredientDto>();
        foreach (var ingredient in ingredients)
        {
            results.Add(await ParseAsync(groupId, ingredient, ct));
        }
        return results;
    }
}
