using Mealie.Infrastructure.Parser;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using brute-force tokenization against the group's unit/food DB.
/// </summary>
public class BruteParserStrategy(UnitMatcher unitMatcher, FoodMatcher foodMatcher) : IIngredientParserStrategy
{
    public async Task<IList<ParsedIngredientDto>?> ParseBatchAsync(
        Guid groupId, IList<string> ingredients, CancellationToken ct = default)
    {
        var results = new List<ParsedIngredientDto>(ingredients.Count);
        foreach (var ingredient in ingredients)
            results.Add(await ParseSingleAsync(groupId, ingredient, ct));
        return results;
    }

    internal async Task<ParsedIngredientDto> ParseSingleAsync(
        Guid groupId, string ingredientString, CancellationToken ct)
    {
        var normalized = IngredientNormalizer.Normalize(ingredientString);
        var (quantity, afterQuantity) = QuantityTokenizer.Tokenize(normalized);
        var (unit, afterUnit) = await unitMatcher.MatchAsync(groupId, afterQuantity, ct);
        var (food, note) = await foodMatcher.MatchAsync(groupId, afterUnit, ct);

        var qConf = quantity.HasValue ? 1.0 : 0.0;
        var uConf = unit is not null ? 1.0 : 0.0;
        var fConf = food is not null ? 1.0 : 0.0;

        return new ParsedIngredientDto
        {
            Input = ingredientString,
            Confidence = new IngredientConfidenceDto
            {
                Average = (qConf + uConf + fConf) / 3.0,
                Quantity = qConf,
                Unit = uConf,
                Food = fConf
            },
            Ingredient = new ParsedIngredientIngredientDto
            {
                Quantity = quantity,
                Unit = unit is not null ? new ParsedIngredientUnitDto { Id = unit.Id, Name = unit.Name } : null,
                Food = food is not null ? new ParsedIngredientFoodDto { Id = food.Id, Name = food.Name } : null,
                Note = string.IsNullOrEmpty(note) ? null : note,
                Display = normalized,
                OriginalText = ingredientString
            }
        };
    }
}
