using Mealie.Infrastructure.Admin;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Parser;

public interface IParserStrategyResolver
{
    /// <summary>
    /// Resolves a parser key to a strategy.
    /// </summary>
    /// <param name="parserKey">"nlp" | "brute" | AI config UUID | null (uses site default)</param>
    Task<IIngredientParserStrategy> ResolveAsync(string? parserKey, CancellationToken ct = default);
}

public class ParserStrategyResolver(
    NlpParserStrategy nlp,
    BruteParserStrategy brute,
    ApplicationDbContext db,
    IApiKeyEncryptionService encryption,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory) : IParserStrategyResolver
{
    public async Task<IIngredientParserStrategy> ResolveAsync(string? parserKey, CancellationToken ct = default)
    {
        var key = parserKey ?? await GetSiteDefaultAsync(ct);

        return key switch
        {
            "nlp" or "nlp-brute" => nlp,
            "brute" => brute,
            _ when Guid.TryParse(key, out var id) => await ResolveAiStrategyAsync(id, ct),
            _ => nlp // unknown key → fall back to NLP
        };
    }

    private async Task<string> GetSiteDefaultAsync(CancellationToken ct)
    {
        var settings = await db.SiteSettings.FirstOrDefaultAsync(ct);
        return settings?.DefaultParser ?? "nlp";
    }

    private async Task<IIngredientParserStrategy> ResolveAiStrategyAsync(Guid configId, CancellationToken ct)
    {
        var config = await db.AiConfigurations.FindAsync([configId], ct);
        if (config is null)
        {
            loggerFactory.CreateLogger<ParserStrategyResolver>()
                .LogWarning("AI config {ConfigId} not found, falling back to NLP", configId);
            return nlp;
        }

        // System prompts are global site settings, not per-provider
        var siteSettings = await db.SiteSettings.FirstOrDefaultAsync(ct);

        var apiKey = encryption.Decrypt(config.EncryptedApiKey) ?? string.Empty;
        var aiConfig = new AiParserConfig(
            ApiKey: apiKey,
            BaseUrl: config.BaseUrl,
            Model: config.DefaultModel ?? "gpt-4o-mini",
            ProjectId: config.ProjectId,
            IngredientSystemPrompt: siteSettings?.IngredientSystemPrompt,
            CategorySystemPrompt: siteSettings?.CategorySystemPrompt,
            TagSystemPrompt: siteSettings?.TagSystemPrompt);

        return config.ProviderType switch
        {
            "openAi" => new OpenAiParserStrategy(aiConfig, httpClientFactory,
                loggerFactory.CreateLogger<OpenAiParserStrategy>()),
            "azureOpenAi" => new AzureOpenAiParserStrategy(aiConfig, httpClientFactory,
                loggerFactory.CreateLogger<AzureOpenAiParserStrategy>()),
            "ollama" => new OllamaParserStrategy(aiConfig, httpClientFactory,
                loggerFactory.CreateLogger<OllamaParserStrategy>()),
            "custom" => new CustomAiParserStrategy(aiConfig, httpClientFactory,
                loggerFactory.CreateLogger<CustomAiParserStrategy>()),
            _ => new CustomAiParserStrategy(aiConfig, httpClientFactory,
                loggerFactory.CreateLogger<CustomAiParserStrategy>())
        };
    }
}
