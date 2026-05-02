using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

public class IngredientParserService(
    IParserStrategyResolver resolver,
    NlpParserStrategy nlpFallback,
    ILogger<IngredientParserService> logger) : IIngredientParserService
{
    public async Task<ParsedIngredientDto> ParseAsync(Guid groupId, string ingredientString,
        string? parserKey = null, CancellationToken ct = default)
    {
        var results = await ParseBatchAsync(groupId, [ingredientString], parserKey, ct);
        return results[0];
    }

    public async Task<IList<ParsedIngredientDto>> ParseBatchAsync(Guid groupId, IList<string> ingredients,
        string? parserKey = null, CancellationToken ct = default)
    {
        var strategy = await resolver.ResolveAsync(parserKey, ct);
        var results = await strategy.ParseBatchAsync(groupId, ingredients, ct);

        if (results is not null && results.Count == ingredients.Count)
            return results;

        // Strategy returned null or wrong count — fall back to NLP
        logger.LogWarning(
            "Strategy {Strategy} failed or returned wrong count for {Count} ingredients, falling back to NLP",
            strategy.GetType().Name, ingredients.Count);

        var fallback = await nlpFallback.ParseBatchAsync(groupId, ingredients, ct);
        return fallback!;
    }
}
