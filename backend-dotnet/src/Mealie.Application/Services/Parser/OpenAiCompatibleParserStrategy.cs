using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

/// <summary>
/// Base strategy for any provider that exposes an OpenAI-compatible
/// chat completions endpoint (OpenAI, Azure OpenAI, Ollama, custom).
/// Concrete subclasses configure the client (headers, base URL, etc.).
/// </summary>
public abstract class OpenAiCompatibleParserStrategy(
    AiParserConfig config,
    IHttpClientFactory httpClientFactory,
    ILogger logger) : IIngredientParserStrategy
{
    public async Task<IList<ParsedIngredientDto>?> ParseBatchAsync(
        Guid groupId, IList<string> ingredients, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            logger.LogWarning("{Strategy}: API key is empty after decryption, skipping", GetType().Name);
            return null;
        }

        try
        {
            var client = BuildClient();
            var numbered = string.Join("\n", ingredients.Select((s, i) => $"{i + 1}. {s}"));

            var requestBody = new
            {
                model = config.Model,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = """
                                  You are a recipe ingredient parser. Given a numbered list of ingredient strings,
                                  extract the structured data for each one and return a JSON object with a single key
                                  "ingredients" whose value is an array of objects — one per input line, in the same order.
                                  Each object must have exactly these fields:
                                  - quantity: number or null
                                  - unit: string or null (the unit of measure, e.g. "cup", "tablespoon")
                                  - food: string or null (the main ingredient, e.g. "flour", "butter")
                                  - note: string or null (preparation notes, e.g. "finely chopped", "room temperature")
                                  The array length must equal the number of input lines.
                                  """
                    },
                    new { role = "user", content = numbered }
                }
            };

            var response = await client.PostAsJsonAsync("chat/completions", requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "{Strategy}: HTTP {StatusCode} (model={Model}, count={Count}): {ErrorBody}",
                    GetType().Name, (int)response.StatusCode, config.Model, ingredients.Count,
                    errorBody.Length > 500 ? errorBody[..500] : errorBody);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (content is null) return null;

            var parsed = JsonSerializer.Deserialize<JsonElement>(content);
            if (!parsed.TryGetProperty("ingredients", out var arr) || arr.ValueKind != JsonValueKind.Array)
            {
                logger.LogWarning("{Strategy}: response missing 'ingredients' array", GetType().Name);
                return null;
            }

            var items = arr.EnumerateArray().ToList();
            if (items.Count != ingredients.Count)
            {
                logger.LogWarning("{Strategy}: expected {Expected} items, got {Got}",
                    GetType().Name, ingredients.Count, items.Count);
                return null;
            }

            var results = new List<ParsedIngredientDto>(ingredients.Count);
            for (var i = 0; i < ingredients.Count; i++)
            {
                var item = items[i];
                results.Add(new ParsedIngredientDto
                {
                    Input = ingredients[i],
                    Confidence = new IngredientConfidenceDto { Average = 1.0, Quantity = 1.0, Unit = 1.0, Food = 1.0 },
                    Ingredient = new ParsedIngredientIngredientDto
                    {
                        Quantity = GetDecimal(item, "quantity"),
                        Unit = GetString(item, "unit") is { } u ? new ParsedIngredientUnitDto { Name = u } : null,
                        Food = GetString(item, "food") is { } f ? new ParsedIngredientFoodDto { Name = f } : null,
                        Note = GetString(item, "note"),
                        Display = ingredients[i],
                        OriginalText = ingredients[i]
                    }
                });
            }

            logger.LogInformation("{Strategy}: parsed {Count} ingredients (model={Model})",
                GetType().Name, results.Count, config.Model);
            return results;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{Strategy}: batch parse request failed", GetType().Name);
            return null;
        }
    }

    /// <summary>Builds and configures the HttpClient for this provider.</summary>
    protected abstract HttpClient BuildClient();

    private static decimal? GetDecimal(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? (decimal?)v.GetDecimal()
            : null;

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
