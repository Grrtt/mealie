using System.Net.Http.Json;
using System.Text;
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
    ParserClientFactory clientFactory,
    ParserProviderDescriptor providerDescriptor,
    ILogger logger) : IIngredientParserStrategy, IOrganizerAiStrategy
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
            var client = clientFactory.CreateClient(config, providerDescriptor);
            var numbered = string.Join("\n", ingredients.Select((s, i) => $"{i + 1}. {s}"));

            var systemPrompt = config.IngredientSystemPrompt ?? DefaultAiParserPrompts.Ingredient;

            var requestBody = new
            {
                model = config.Model,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
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

    public async Task<RecipeOrganizerSuggestions?> SuggestOrganizersAsync(
        RecipeOrganizerContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            logger.LogWarning("{Strategy}: API key is empty, skipping organizer suggestions", GetType().Name);
            return null;
        }

        try
        {
            var client = clientFactory.CreateClient(config, providerDescriptor);

            var categoryInstructions = config.CategorySystemPrompt ?? DefaultAiParserPrompts.Category;

            var tagInstructions = config.TagSystemPrompt ?? DefaultAiParserPrompts.Tag;

            var systemPrompt = $"""
                You are a recipe organizer. Given recipe details and its existing website categories/tags,
                suggest additional labels to enrich it.
                Return a JSON object with exactly two fields:
                - "categories": array of category strings
                - "tags": array of tag strings

                ## Category Instructions
                {categoryInstructions}

                ## Tag Instructions
                {tagInstructions}
                """;

            var sb = new StringBuilder();
            sb.AppendLine($"Recipe: {context.RecipeName}");
            if (!string.IsNullOrWhiteSpace(context.Description))
                sb.AppendLine($"Description: {context.Description}");
            if (context.Ingredients.Count > 0)
            {
                sb.AppendLine("\nIngredients:");
                foreach (var ing in context.Ingredients)
                    sb.AppendLine($"- {ing}");
            }
            if (context.WebsiteCategories.Count > 0)
                sb.AppendLine($"\nWebsite Categories: {string.Join(", ", context.WebsiteCategories)}");
            if (context.WebsiteTags.Count > 0)
                sb.AppendLine($"\nWebsite Tags: {string.Join(", ", context.WebsiteTags)}");

            var requestBody = new
            {
                model = config.Model,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = sb.ToString() }
                }
            };

            var response = await client.PostAsJsonAsync("chat/completions", requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "{Strategy}: organizer HTTP {StatusCode}: {ErrorBody}",
                    GetType().Name, (int)response.StatusCode,
                    errorBody.Length > 500 ? errorBody[..500] : errorBody);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var content = json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (content is null) return null;

            var parsed = JsonSerializer.Deserialize<JsonElement>(content);
            var categories = ParseStringArray(parsed, "categories");
            var tags = ParseStringArray(parsed, "tags");

            logger.LogInformation("{Strategy}: organizer suggested {CatCount} categories, {TagCount} tags (recipe={Recipe})",
                GetType().Name, categories.Count, tags.Count, context.RecipeName);

            return new RecipeOrganizerSuggestions(categories, tags);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{Strategy}: organizer request failed", GetType().Name);
            return null;
        }
    }

    private static List<string> ParseStringArray(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];
        return arr.EnumerateArray()
            .Where(v => v.ValueKind == JsonValueKind.String)
            .Select(v => v.GetString()!)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static decimal? GetDecimal(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number
            ? (decimal?)v.GetDecimal()
            : null;

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
