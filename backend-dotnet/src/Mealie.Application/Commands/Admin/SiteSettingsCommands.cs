using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Application.Services.Parser;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Admin;

// ── Get ───────────────────────────────────────────────────────────────────────

public record GetSiteSettingsQuery : IQuery<SiteSettingsResponse>
{
    public async Task<SiteSettingsResponse> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var settings = await GetOrCreateAsync(services, ct);
        return await BuildResponseAsync(settings, services, ct);
    }

    internal static async Task<SiteSettings> GetOrCreateAsync(IQueryServices services, CancellationToken ct)
    {
        var existing = await services.Db.SiteSettings.FirstOrDefaultAsync(ct);
        if (existing is not null) return existing;

        var newSettings = new SiteSettings
        {
            Id = Guid.NewGuid(),
            DefaultParser = "nlp",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        services.Db.SiteSettings.Add(newSettings);
        await services.Db.SaveChangesAsync(ct);
        return newSettings;
    }

    internal static async Task<SiteSettingsResponse> BuildResponseAsync(SiteSettings settings,
        IQueryServices services, CancellationToken ct)
    {
        bool defaultParserUnavailable = false;
        var parser = settings.DefaultParser;

        if (Guid.TryParse(parser, out var configId))
        {
            var configExists = await services.Db.AiConfigurations
                .AnyAsync(c => c.Id == configId, ct);
            if (!configExists)
            {
                defaultParserUnavailable = true;
            }
        }

        return new SiteSettingsResponse
        {
            DefaultParser = parser,
            DefaultParserUnavailable = defaultParserUnavailable,
            IngredientSystemPrompt = settings.IngredientSystemPrompt,
            CategorySystemPrompt = settings.CategorySystemPrompt,
            TagSystemPrompt = settings.TagSystemPrompt,
            DefaultIngredientSystemPrompt = DefaultAiParserPrompts.Ingredient,
            DefaultCategorySystemPrompt = DefaultAiParserPrompts.Category,
            DefaultTagSystemPrompt = DefaultAiParserPrompts.Tag,
        };
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public record UpdateSiteSettingsCommand(UpdateSiteSettingsRequest Request)
    : IQuery<(SiteSettingsResponse? Response, string? Error)>
{
    public async Task<(SiteSettingsResponse? Response, string? Error)> ExecuteAsync(
        IQueryServices services, CancellationToken ct = default)
    {
        var newParser = Request.DefaultParser;

        // Validate the value
        if (newParser != "nlp" && newParser != "brute")
        {
            if (!Guid.TryParse(newParser, out var configId))
            {
                return (null,
                    "defaultParser must be 'nlp', 'brute', or a valid AI configuration UUID.");
            }

            var configExists = await services.Db.AiConfigurations
                .AnyAsync(c => c.Id == configId, ct);
            if (!configExists)
            {
                return (null,
                    $"No AI configuration found with ID '{newParser}'.");
            }
        }

        // Load current values so we can preserve unchanged prompts (null in request = no change)
        var current = await services.Db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);

        static string? ResolvePrompt(string? requestValue, string? currentValue) =>
            requestValue is null ? currentValue : (requestValue == "" ? null : requestValue);

        var ingredientPrompt = ResolvePrompt(Request.IngredientSystemPrompt, current?.IngredientSystemPrompt);
        var categoryPrompt   = ResolvePrompt(Request.CategorySystemPrompt,   current?.CategorySystemPrompt);
        var tagPrompt        = ResolvePrompt(Request.TagSystemPrompt,        current?.TagSystemPrompt);

        // Single bulk update — bypasses change-tracker to avoid DbUpdateConcurrencyException
        var rowsUpdated = await services.Db.SiteSettings
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.DefaultParser,          newParser)
                .SetProperty(e => e.IngredientSystemPrompt, ingredientPrompt)
                .SetProperty(e => e.CategorySystemPrompt,   categoryPrompt)
                .SetProperty(e => e.TagSystemPrompt,        tagPrompt)
                .SetProperty(e => e.UpdatedAt,              DateTime.UtcNow), ct);

        if (rowsUpdated == 0)
        {
            services.Db.SiteSettings.Add(new SiteSettings
            {
                Id = Guid.NewGuid(),
                DefaultParser          = newParser,
                IngredientSystemPrompt = ingredientPrompt,
                CategorySystemPrompt   = categoryPrompt,
                TagSystemPrompt        = tagPrompt,
                CreatedAt              = DateTime.UtcNow,
                UpdatedAt              = DateTime.UtcNow
            });
            await services.Db.SaveChangesAsync(ct);
        }

        // Sync is_active on AI configurations
        if (Guid.TryParse(newParser, out var activeId))
        {
            await services.Db.AiConfigurations
                .Where(c => c.IsActive && c.Id != activeId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), ct);

            await services.Db.AiConfigurations
                .Where(c => c.Id == activeId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, true), ct);
        }
        else
        {
            await services.Db.AiConfigurations
                .Where(c => c.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), ct);
        }

        // Build response from fresh DB read
        var updatedSettings = await services.Db.SiteSettings.FirstAsync(ct);
        var response = await GetSiteSettingsQuery.BuildResponseAsync(updatedSettings, services, ct);
        return (response, null);
    }
}
