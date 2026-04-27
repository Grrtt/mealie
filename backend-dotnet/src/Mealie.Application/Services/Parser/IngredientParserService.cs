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

        var quantityConf = quantity.HasValue ? 1.0 : 0.0;
        var unitConf = unit is not null ? 1.0 : 0.0;
        var foodConf = food is not null ? 1.0 : 0.0;
        var average = (quantityConf + unitConf + foodConf) / 3.0;

        return new ParsedIngredientDto
        {
            Input = ingredientString,
            Confidence = new IngredientConfidenceDto
            {
                Average = average,
                Quantity = quantityConf,
                Unit = unitConf,
                Food = foodConf,
            },
            Ingredient = new ParsedIngredientIngredientDto
            {
                Quantity = quantity,
                Unit = unit is not null ? new ParsedIngredientUnitDto { Id = unit.Id, Name = unit.Name } : null,
                Food = food is not null ? new ParsedIngredientFoodDto { Id = food.Id, Name = food.Name } : null,
                Note = string.IsNullOrEmpty(note) ? null : note,
                Display = ingredientString,
                OriginalText = ingredientString,
            }
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
