using System.Net.Http.Json;
using System.Text.Json;
using Mealie.Application.Services.IngredientParser;
using Mealie.Infrastructure.Configuration;
using Mealie.Infrastructure.Parser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Parser;

public class IngredientParserService(
    UnitMatcher unitMatcher,
    FoodMatcher foodMatcher,
    IngredientParser.IngredientParserService nlpParserService,
    IHttpClientFactory httpClientFactory,
    IOptions<AppSettings> appSettings,
    ILogger<IngredientParserService> logger) : IIngredientParserService
{
    public async Task<ParsedIngredientDto> ParseAsync(Guid groupId, string ingredientString,
        string parserName = "nlp", CancellationToken ct = default)
    {
        if (parserName == "openai" && !string.IsNullOrEmpty(appSettings.Value.OpenAiApiKey))
        {
            var openAiResult = await ParseWithOpenAiAsync(ingredientString, ct);
            if (openAiResult is not null)
            {
                return openAiResult;
            }

            logger.LogWarning("OpenAI parse failed for '{Ingredient}', falling back to brute parser", ingredientString);
        }
        else if (parserName is "nlp" or "nlp-brute")
        {
            var nlpResults = await nlpParserService.ParseBatchAsync([ingredientString], ct);
            if (nlpResults.Count > 0 && nlpResults[0].Food is not null)
            {
                return MapNlpResult(nlpResults[0]);
            }
        }

        return await ParseBruteAsync(groupId, ingredientString, ct);
    }

    public async Task<IList<ParsedIngredientDto>> ParseBatchAsync(Guid groupId, IList<string> ingredients,
        string parserName = "nlp", CancellationToken ct = default)
    {
        if (parserName == "openai" && !string.IsNullOrEmpty(appSettings.Value.OpenAiApiKey))
        {
            var results = new List<ParsedIngredientDto>();
            foreach (var ingredient in ingredients)
            {
                results.Add(await ParseAsync(groupId, ingredient, parserName, ct));
            }

            return results;
        }

        if (parserName is "nlp" or "nlp-brute")
        {
            var nlpResults = await nlpParserService.ParseBatchAsync(ingredients, ct);
            return nlpResults.Select(MapNlpResult).ToList();
        }

        var bruteResults = new List<ParsedIngredientDto>();
        foreach (var ingredient in ingredients)
        {
            bruteResults.Add(await ParseBruteAsync(groupId, ingredient, ct));
        }

        return bruteResults;
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

    private async Task<ParsedIngredientDto?> ParseWithOpenAiAsync(string ingredientString, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("OpenAi");
            var requestBody = new
            {
                model = "gpt-4o-mini",
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = """
                                  You are a recipe ingredient parser. Given an ingredient string, extract the structured data.
                                  Respond with a JSON object containing exactly these fields:
                                  - quantity: number or null
                                  - unit: string or null (the unit of measure, e.g. "cup", "tablespoon")
                                  - food: string or null (the main ingredient, e.g. "flour", "butter")
                                  - note: string or null (preparation notes, e.g. "finely chopped", "room temperature")
                                  """
                    },
                    new { role = "user", content = ingredientString }
                }
            };

            var response = await client.PostAsJsonAsync("chat/completions", requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (content is null)
            {
                return null;
            }

            var parsed = JsonSerializer.Deserialize<JsonElement>(content);
            var quantity = parsed.TryGetProperty("quantity", out var q) && q.ValueKind == JsonValueKind.Number
                ? (decimal?)q.GetDecimal()
                : null;
            var unit = parsed.TryGetProperty("unit", out var u) && u.ValueKind == JsonValueKind.String
                ? u.GetString()
                : null;
            var food = parsed.TryGetProperty("food", out var f) && f.ValueKind == JsonValueKind.String
                ? f.GetString()
                : null;
            var note = parsed.TryGetProperty("note", out var n) && n.ValueKind == JsonValueKind.String
                ? n.GetString()
                : null;

            return new ParsedIngredientDto
            {
                Input = ingredientString,
                Confidence = new IngredientConfidenceDto { Average = 1.0, Quantity = 1.0, Unit = 1.0, Food = 1.0 },
                Ingredient = new ParsedIngredientIngredientDto
                {
                    Quantity = quantity,
                    Unit = unit is not null ? new ParsedIngredientUnitDto { Name = unit } : null,
                    Food = food is not null ? new ParsedIngredientFoodDto { Name = food } : null,
                    Note = note,
                    Display = ingredientString,
                    OriginalText = ingredientString
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenAI ingredient parse request failed for '{Ingredient}'", ingredientString);
            return null;
        }
    }
}
