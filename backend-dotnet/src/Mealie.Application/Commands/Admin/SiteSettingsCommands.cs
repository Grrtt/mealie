using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
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
        var legacyEnvVarsDetected = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        bool defaultParserUnavailable = false;
        var parser = settings.DefaultParser;

        // If it looks like a UUID, verify the AI config still exists
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
            LegacyEnvVarsDetected = legacyEnvVarsDetected
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

        // Update site settings
        var settings = await GetSiteSettingsQuery.GetOrCreateAsync(services, ct);
        settings.DefaultParser = newParser;
        settings.UpdatedAt = DateTime.UtcNow;

        // Sync is_active on AI configurations
        if (Guid.TryParse(newParser, out var activeId))
        {
            // Activate the referenced config, deactivate all others
            await services.Db.AiConfigurations
                .Where(c => c.IsActive && c.Id != activeId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), ct);

            await services.Db.AiConfigurations
                .Where(c => c.Id == activeId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, true), ct);
        }
        else
        {
            // Built-in parser selected — deactivate all AI configs
            await services.Db.AiConfigurations
                .Where(c => c.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), ct);
        }

        await services.Db.SaveChangesAsync(ct);

        var response = await GetSiteSettingsQuery.BuildResponseAsync(settings, services, ct);
        return (response, null);
    }
}
