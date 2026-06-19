using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Mealie.Application.Services.Parser;
using Mealie.Domain.Entities.Settings;
using Mealie.Infrastructure.Admin;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Scraper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Ocr;

/// <summary>
/// Uses an OpenAI-compatible vision API to extract a structured recipe
/// (name, description, ingredients, instructions) from an uploaded image.
/// </summary>
public class OcrService(
    ApplicationDbContext db,
    IApiKeyEncryptionService encryption,
    IHttpClientFactory httpClientFactory,
    ILogger<OcrService> logger) : IOcrService
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47];

    public async Task<ScrapedRecipeDto?> ExtractRecipeFromImageAsync(
        byte[] imageBytes, CancellationToken ct = default)
    {
        var config = await db.AiConfigurations.FirstOrDefaultAsync(c => c.IsActive, ct);
        if (config is null)
        {
            logger.LogWarning("OCR: no active AI configuration found");
            return null;
        }

        if (!config.EnableImageServices)
        {
            logger.LogWarning("OCR: image services are disabled for the active AI configuration");
            return null;
        }

        var apiKey = encryption.Decrypt(config.EncryptedApiKey) ?? string.Empty;
        if (string.IsNullOrEmpty(apiKey) && config.ProviderType != "ollama")
        {
            logger.LogWarning("OCR: API key is empty after decryption");
            return null;
        }

        var aiConfig = new AiParserConfig(
            ApiKey: apiKey,
            BaseUrl: config.BaseUrl,
            Model: config.DefaultModel ?? "gpt-4o",
            ProjectId: config.ProjectId);

        var descriptor = config.ProviderType switch
        {
            "openAi" => ParserProviderDescriptor.OpenAi,
            "azureOpenAi" => ParserProviderDescriptor.AzureOpenAi,
            "ollama" => ParserProviderDescriptor.Ollama,
            "custom" => ParserProviderDescriptor.Custom,
            _ => ParserProviderDescriptor.Custom
        };

        // Determine image MIME type from header bytes
        var mimeType = imageBytes.Length >= 4
                       && imageBytes.AsSpan(0, 4).SequenceEqual(PngHeader)
            ? "image/png"
            : "image/jpeg";

        var base64Image = Convert.ToBase64String(imageBytes);
        var dataUrl = $"data:{mimeType};base64,{base64Image}";

        var requestBody = new JsonObject
        {
            ["model"] = aiConfig.Model,
            ["max_tokens"] = 4096,
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = ExtractPrompt
                },
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = "Extract the recipe from this image."
                        },
                        new JsonObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JsonObject { ["url"] = dataUrl }
                        }
                    }
                }
            }
        };

        try
        {
            var clientFactory = new ParserClientFactory(httpClientFactory);
            using var client = clientFactory.CreateClient(aiConfig, descriptor);

            var response = await client.PostAsJsonAsync("chat/completions", requestBody, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "OCR: HTTP {StatusCode} (model={Model}): {Error}",
                    (int)response.StatusCode, aiConfig.Model,
                    errorBody.Length > 500 ? errorBody[..500] : errorBody);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            var choices = json.GetProperty("choices");
            if (choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
            {
                logger.LogWarning("OCR: response has no choices");
                return null;
            }

            var content = choices[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (content is null)
            {
                logger.LogWarning("OCR: response content is null");
                return null;
            }

            var parsed = JsonSerializer.Deserialize<JsonElement>(content);

            var scraped = new ScrapedRecipeDto
            {
                Name = GetString(parsed, "name"),
                Description = GetString(parsed, "description"),
                RecipeYield = GetString(parsed, "recipeYield"),
                TotalTime = GetString(parsed, "totalTime"),
                PrepTime = GetString(parsed, "prepTime"),
                CookTime = GetString(parsed, "cookTime"),
                RecipeIngredient = ParseStringList(parsed, "recipeIngredient"),
                RecipeInstructions = ParseStringList(parsed, "recipeInstructions"),
                Keywords = ParseStringList(parsed, "tags"),
                Categories = ParseStringList(parsed, "categories"),
                Nutrition = ParseNutrition(parsed),
                ScrapingNotSupported = false
            };

            logger.LogInformation(
                "OCR: extracted recipe '{Name}' with {IngredientCount} ingredients and {InstructionCount} instructions",
                scraped.Name ?? "(unnamed)",
                scraped.RecipeIngredient.Count,
                scraped.RecipeInstructions.Count);

            return scraped;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OCR: request failed (model={Model})", aiConfig.Model);
            return null;
        }
    }

    private static string? GetString(JsonElement el, string propertyName)
    {
        // The model might return camelCase (recipeIngredient) or snake_case (recipe_ingredient)
        // Try the exact name first, then snake_case fallback.
        if (el.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();

        var snake = string.Concat(propertyName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : c.ToString()));
        if (snake != propertyName && el.TryGetProperty(snake, out v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();

        return null;
    }

    private static IList<string> ParseStringList(JsonElement el, string propertyName)
    {
        // Try the exact property name first, then snake_case fallback.
        if (TryGetArray(el, propertyName, out var arr) ||
            TryGetArray(el, ToSnakeCase(propertyName), out arr))
        {
            return arr.EnumerateArray()
                .Where(v => v.ValueKind == JsonValueKind.String)
                .Select(v => v.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        return [];
    }

    private static NutritionDto? ParseNutrition(JsonElement el)
    {
        // Try "nutrition" then "nutritionData"
        if (!el.TryGetProperty("nutrition", out var n) || n.ValueKind != JsonValueKind.Object)
        {
            if (!el.TryGetProperty("nutritionData", out n) || n.ValueKind != JsonValueKind.Object)
                return null;
        }

        return new NutritionDto
        {
            Calories = GetString(n, "calories"),
            FatContent = GetString(n, "fatContent") ?? GetString(n, "fat_content"),
            ProteinContent = GetString(n, "proteinContent") ?? GetString(n, "protein_content"),
            CarbohydrateContent = GetString(n, "carbohydrateContent") ?? GetString(n, "carbohydrate_content")
        };
    }

    private static bool TryGetArray(JsonElement el, string name, out JsonElement arr)
    {
        if (el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array)
        {
            arr = v;
            return true;
        }

        arr = default;
        return false;
    }

    private static string ToSnakeCase(string input) =>
        string.Concat(input.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : c.ToString()));

    private const string ExtractPrompt = """
                                         You are a recipe extraction assistant. Given an image containing a recipe
                                         (cookbook page, handwritten note, screenshot, label, etc.), extract the
                                         recipe information and return it as a JSON object with exactly these fields:

                                         - name: string (the recipe title)
                                         - description: string or null (short summary)
                                         - recipeYield: string or null (e.g. "4 servings")
                                         - totalTime: string or null (e.g. "30 minutes")
                                         - prepTime: string or null (e.g. "10 minutes")
                                         - cookTime: string or null (e.g. "20 minutes")
                                         - recipeIngredient: array of strings (one ingredient per entry)
                                         - recipeInstructions: array of strings (one step per entry)
                                         - tags: array of strings (optional tags/cuisine labels)
                                         - categories: array of strings (e.g. "Dinner", "Dessert")
                                         - nutrition: object or null with optional fields: calories, fatContent,
                                           proteinContent, carbohydrateContent (all strings)

                                         If the image does not appear to contain a recipe, return an empty JSON object.
                                         Use the original language of the recipe for all extracted text.
                                         """;
}
