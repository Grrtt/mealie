namespace Mealie.Application.Services.Parser;

/// <summary>
/// Strategy for parsing a batch of ingredient strings into structured data.
/// Each strategy corresponds to a parser backend (NLP, brute, or an AI provider).
/// </summary>
public interface IIngredientParserStrategy
{
    /// <summary>
    /// Parses a batch of ingredient strings.
    /// Returns null if the strategy fails and the caller should fall back.
    /// </summary>
    Task<IList<ParsedIngredientDto>?> ParseBatchAsync(
        Guid groupId, IList<string> ingredients, CancellationToken ct = default);
}
