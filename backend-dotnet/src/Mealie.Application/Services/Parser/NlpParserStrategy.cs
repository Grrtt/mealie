using Mealie.Application.Services.IngredientParser;
using Mealie.Infrastructure.Parser;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Parses ingredients using the CRF-based NLP model, with brute-force fallback per ingredient.
/// </summary>
public class NlpParserStrategy(
    IngredientParser.IngredientParserService nlpService,
    BruteParserStrategy brute) : IIngredientParserStrategy
{
    public async Task<IList<ParsedIngredientDto>?> ParseBatchAsync(
        Guid groupId, IList<string> ingredients, CancellationToken ct = default)
    {
        var nlpResults = await nlpService.ParseBatchAsync(ingredients, ct);
        var results = new List<ParsedIngredientDto>(ingredients.Count);

        for (var i = 0; i < ingredients.Count; i++)
        {
            var nlp = nlpResults[i];
            if (nlp.Food is not null)
            {
                results.Add(MapNlpResult(nlp));
            }
            else
            {
                // NLP couldn't find the food — fall back to brute for this ingredient
                var bruteResult = await brute.ParseSingleAsync(groupId, ingredients[i], ct);
                results.Add(bruteResult);
            }
        }

        return results;
    }

    internal static ParsedIngredientDto MapNlpResult(ParsedIngredientResult nlp) =>
        new()
        {
            Input = nlp.Input,
            Confidence = new IngredientConfidenceDto
            {
                Average = nlp.Food is not null ? 1.0 : 0.5,
                Quantity = nlp.Quantity.HasValue ? 1.0 : 0.0,
                Unit = nlp.Unit is not null ? 1.0 : 0.0,
                Food = nlp.Food is not null ? 1.0 : 0.0
            },
            Ingredient = new ParsedIngredientIngredientDto
            {
                Quantity = nlp.Quantity.HasValue ? (decimal)nlp.Quantity.Value : null,
                Unit = nlp.Unit is not null ? new ParsedIngredientUnitDto { Name = nlp.Unit } : null,
                Food = nlp.Food is not null ? new ParsedIngredientFoodDto { Name = nlp.Food } : null,
                Note = nlp.Note,
                Display = nlp.Input,
                OriginalText = nlp.Input
            }
        };
}
