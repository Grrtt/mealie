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
            DefaultParserUnavailable = defaultParserUnavailable
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

        // Update site settings using bulk SQL to avoid change-tracker concurrency issues
        var rowsUpdated = await services.Db.SiteSettings
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.DefaultParser, newParser)
                .SetProperty(e => e.UpdatedAt, DateTime.UtcNow), ct);

        // If no row existed yet, create one
        if (rowsUpdated == 0)
        {
            services.Db.SiteSettings.Add(new SiteSettings
            {
                Id = Guid.NewGuid(),
                DefaultParser = newParser,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
