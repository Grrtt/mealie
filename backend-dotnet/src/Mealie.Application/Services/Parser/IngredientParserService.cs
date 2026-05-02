using System.Net.Http.Json;
using System.Text.Json;
using Mealie.Application.Services.IngredientParser;
using Mealie.Infrastructure.Admin;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

public class IngredientParserService(
    UnitMatcher unitMatcher,
    FoodMatcher foodMatcher,
    IngredientParser.IngredientParserService nlpParserService,
    IHttpClientFactory httpClientFactory,
    ApplicationDbContext db,
    IApiKeyEncryptionService encryptionService,
    ILogger<IngredientParserService> logger) : IIngredientParserService
{
    public async Task<ParsedIngredientDto> ParseAsync(Guid groupId, string ingredientString,
        string? parserKey = null, CancellationToken ct = default)
    {
        var resolvedParser = await ResolveParserAsync(parserKey, ct);

        if (resolvedParser == "brute")
        {
            return await ParseBruteAsync(groupId, ingredientString, ct);
        }

        if (resolvedParser == "nlp")
        {
            var nlpResults = await nlpParserService.ParseBatchAsync([ingredientString], ct);
            if (nlpResults.Count > 0 && nlpResults[0].Food is not null)
            {
                return MapNlpResult(nlpResults[0]);
            }

            return await ParseBruteAsync(groupId, ingredientString, ct);
        }

        // UUID → AI provider
        if (Guid.TryParse(resolvedParser, out var configId))
        {
            var openAiResult = await ParseWithAiConfigAsync(configId, ingredientString, ct);
            if (openAiResult is not null)
            {
                return openAiResult;
            }

            logger.LogWarning(
                "AI parse failed for '{Ingredient}' using config {ConfigId}, falling back to NLP",
                ingredientString, configId);
        }

        // Fallback to NLP
        var fallbackResults = await nlpParserService.ParseBatchAsync([ingredientString], ct);
        if (fallbackResults.Count > 0 && fallbackResults[0].Food is not null)
        {
            return MapNlpResult(fallbackResults[0]);
        }

        return await ParseBruteAsync(groupId, ingredientString, ct);
    }

    public async Task<IList<ParsedIngredientDto>> ParseBatchAsync(Guid groupId, IList<string> ingredients,
        string? parserKey = null, CancellationToken ct = default)
    {
        var resolvedParser = await ResolveParserAsync(parserKey, ct);

        if (resolvedParser is "nlp" or "nlp-brute")
        {
            var nlpResults = await nlpParserService.ParseBatchAsync(ingredients, ct);
            return nlpResults.Select(MapNlpResult).ToList();
        }

        if (resolvedParser == "brute")
        {
            var bruteResults = new List<ParsedIngredientDto>();
            foreach (var ingredient in ingredients)
            {
                bruteResults.Add(await ParseBruteAsync(groupId, ingredient, ct));
            }

            return bruteResults;
        }

        // UUID → AI provider (single batched request)
        if (Guid.TryParse(resolvedParser, out var configId))
        {
            var aiResults = await ParseBatchWithAiConfigAsync(configId, ingredients, ct);
            if (aiResults is not null && aiResults.Count == ingredients.Count)
                return aiResults;

            logger.LogWarning("AI batch parse failed or returned wrong count, falling back to NLP for all ingredients");
        }

        // Fallback to NLP
        var fallback = await nlpParserService.ParseBatchAsync(ingredients, ct);
        return fallback.Select(MapNlpResult).ToList();
    }

    /// <summary>
    ///     Resolves the effective parser key.
    ///     - null → read site_settings.default_parser; fallback to "nlp" if table is empty.
    ///     - "nlp" / "brute" → use as-is.
    ///     - UUID string → use as-is (caller is responsible for valid ID).
    /// </summary>
    private async Task<string> ResolveParserAsync(string? parserKey, CancellationToken ct)
    {
        if (parserKey is not null) return parserKey;

        var settings = await db.SiteSettings.FirstOrDefaultAsync(ct);
        return settings?.DefaultParser ?? "nlp";
    }

    private async Task<ParsedIngredientDto?> ParseWithAiConfigAsync(Guid configId,
        string ingredientString, CancellationToken ct)
    {
        var results = await ParseBatchWithAiConfigAsync(configId, [ingredientString], ct);
        return results?.Count > 0 ? results[0] : null;
    }

    private async Task<List<ParsedIngredientDto>?> ParseBatchWithAiConfigAsync(Guid configId,
        IList<string> ingredients, CancellationToken ct)
    {
        var config = await db.AiConfigurations.FindAsync([configId], ct);
        if (config is null) return null;

        var apiKey = encryptionService.Decrypt(config.EncryptedApiKey);
        var baseUrl = config.BaseUrl;
        var model = config.DefaultModel ?? "gpt-4o-mini";
        var projectId = config.ProjectId;

        return await ParseBatchWithOpenAiAsync(ingredients, apiKey, baseUrl, model, projectId, ct);
    }

    private static ParsedIngredientDto MapNlpResult(ParsedIngredientResult nlp)
    {
        return new ParsedIngredientDto
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

    private async Task<ParsedIngredientDto> ParseBruteAsync(Guid groupId, string ingredientString,
        CancellationToken ct)
    {
        var normalized = IngredientNormalizer.Normalize(ingredientString);
        var (quantity, afterQuantity) = QuantityTokenizer.Tokenize(normalized);
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
                Food = foodConf
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

    private async Task<List<ParsedIngredientDto>?> ParseBatchWithOpenAiAsync(IList<string> ingredients,
        string? apiKey, string? baseUrl, string model, string? projectId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("AI parse skipped: API key is empty after decryption");
            return null;
        }

        try
        {
            var client = httpClientFactory.CreateClient("OpenAi");

            if (!string.IsNullOrEmpty(baseUrl))
            {
                client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            }

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            if (!string.IsNullOrEmpty(projectId))
            {
                client.DefaultRequestHeaders.Remove("OpenAI-Project");
                client.DefaultRequestHeaders.Add("OpenAI-Project", projectId);
            }

            // Build a numbered list so the model returns a positionally-matched array
            var numbered = string.Join("\n", ingredients.Select((s, i) => $"{i + 1}. {s}"));

            var requestBody = new
            {
                model,
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
                    "AI batch parse HTTP {StatusCode} (model={Model}, baseUrl={BaseUrl}, count={Count}): {ErrorBody}",
                    (int)response.StatusCode, model,
                    string.IsNullOrEmpty(baseUrl) ? "default" : baseUrl,
                    ingredients.Count,
                    errorBody.Length > 500 ? errorBody[..500] : errorBody);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (content is null) return null;

            var parsed = JsonSerializer.Deserialize<JsonElement>(content);
            if (!parsed.TryGetProperty("ingredients", out var arr) || arr.ValueKind != JsonValueKind.Array)
            {
                logger.LogWarning("AI batch parse: response missing 'ingredients' array");
                return null;
            }

            var results = new List<ParsedIngredientDto>();
            var items = arr.EnumerateArray().ToList();

            for (var i = 0; i < ingredients.Count; i++)
            {
                var input = ingredients[i];
                if (i >= items.Count)
                {
                    logger.LogWarning("AI batch parse: response array too short (expected {Expected}, got {Got})", ingredients.Count, items.Count);
                    return null;
                }

                var item = items[i];
                var quantity = item.TryGetProperty("quantity", out var q) && q.ValueKind == JsonValueKind.Number
                    ? (decimal?)q.GetDecimal()
                    : null;
                var unit = item.TryGetProperty("unit", out var u) && u.ValueKind == JsonValueKind.String
                    ? u.GetString()
                    : null;
                var food = item.TryGetProperty("food", out var f) && f.ValueKind == JsonValueKind.String
                    ? f.GetString()
                    : null;
                var note = item.TryGetProperty("note", out var n) && n.ValueKind == JsonValueKind.String
                    ? n.GetString()
                    : null;

                results.Add(new ParsedIngredientDto
                {
                    Input = input,
                    Confidence = new IngredientConfidenceDto { Average = 1.0, Quantity = 1.0, Unit = 1.0, Food = 1.0 },
                    Ingredient = new ParsedIngredientIngredientDto
                    {
                        Quantity = quantity,
                        Unit = unit is not null ? new ParsedIngredientUnitDto { Name = unit } : null,
                        Food = food is not null ? new ParsedIngredientFoodDto { Name = food } : null,
                        Note = note,
                        Display = input,
                        OriginalText = input
                    }
                });
            }

            logger.LogInformation("AI batch parse succeeded for {Count} ingredients (model={Model})", results.Count, model);
            return results;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI batch ingredient parse request failed");
            return null;
        }
    }
}
