using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Settings;
using Mealie.Infrastructure.Admin;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Admin;

// ── List ──────────────────────────────────────────────────────────────────────

public record GetAllAiConfigurationsQuery : IQuery<List<AiConfigurationResponse>>
{
    public async Task<List<AiConfigurationResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var configs = await services.Db.AiConfigurations
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

        return configs.Select(c => AiConfigurationMappings.MapToResponse(c, services.EncryptionService)).ToList();
    }
}

// ── Get by ID ─────────────────────────────────────────────────────────────────

public record GetAiConfigurationQuery(Guid Id) : IQuery<AiConfigurationResponse?>
{
    public async Task<AiConfigurationResponse?> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var config = await services.Db.AiConfigurations.FindAsync([Id], ct);
        return config is null ? null : AiConfigurationMappings.MapToResponse(config, services.EncryptionService);
    }
}

// ── Create ────────────────────────────────────────────────────────────────────

public record CreateAiConfigurationCommand(CreateAiConfigurationRequest Request)
    : IQuery<AiConfigurationResponse>
{
    public async Task<AiConfigurationResponse> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var config = new AiConfiguration
        {
            Id = Guid.NewGuid(),
            Name = Request.Name,
            ProviderType = Request.ProviderType,
            EncryptedApiKey = services.EncryptionService.Encrypt(Request.ApiKey),
            BaseUrl = Request.BaseUrl,
            DefaultModel = Request.DefaultModel,
            EnableImageServices = Request.EnableImageServices,
            EnableTranscriptionServices = Request.EnableTranscriptionServices,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        services.Db.AiConfigurations.Add(config);
        await services.Db.SaveChangesAsync(ct);

        return AiConfigurationMappings.MapToResponse(config, services.EncryptionService);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public record UpdateAiConfigurationCommand(Guid Id, UpdateAiConfigurationRequest Request)
    : IQuery<AiConfigurationResponse?>
{
    public async Task<AiConfigurationResponse?> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var config = await services.Db.AiConfigurations.FindAsync([Id], ct);
        if (config is null) return null;

        if (Request.Name is not null) config.Name = Request.Name;

        // API key semantics: null = keep, "" = clear, non-empty = replace
        if (Request.ApiKey is not null)
        {
            config.EncryptedApiKey = Request.ApiKey == string.Empty
                ? null
                : services.EncryptionService.Encrypt(Request.ApiKey);
        }

        if (Request.BaseUrl is not null) config.BaseUrl = Request.BaseUrl;
        if (Request.DefaultModel is not null) config.DefaultModel = Request.DefaultModel;
        if (Request.EnableImageServices.HasValue) config.EnableImageServices = Request.EnableImageServices.Value;
        if (Request.EnableTranscriptionServices.HasValue)
            config.EnableTranscriptionServices = Request.EnableTranscriptionServices.Value;

        config.UpdatedAt = DateTime.UtcNow;
        await services.Db.SaveChangesAsync(ct);

        return AiConfigurationMappings.MapToResponse(config, services.EncryptionService);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public record DeleteAiConfigurationCommand(Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var config = await services.Db.AiConfigurations.FindAsync([Id], ct);
        if (config is null) return false;

        var wasActive = config.IsActive;

        services.Db.AiConfigurations.Remove(config);

        // If this was the active config, reset site settings to "nlp"
        if (wasActive)
        {
            var siteSettings = await GetOrCreateSiteSettingsAsync(services, ct);
            siteSettings.DefaultParser = "nlp";
            siteSettings.UpdatedAt = DateTime.UtcNow;
        }

        await services.Db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<SiteSettings> GetOrCreateSiteSettingsAsync(IQueryServices services,
        CancellationToken ct)
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
        return newSettings;
    }
}

// ── Activate ──────────────────────────────────────────────────────────────────

public record ActivateAiConfigurationCommand(Guid Id) : IQuery<AiConfigurationResponse?>
{
    public async Task<AiConfigurationResponse?> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var config = await services.Db.AiConfigurations.FindAsync([Id], ct);
        if (config is null) return null;

        // Deactivate all, then activate this one
        await services.Db.AiConfigurations
            .Where(c => c.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsActive, false), ct);

        config.IsActive = true;
        config.UpdatedAt = DateTime.UtcNow;

        // Update (or create) site settings to point to this config
        var siteSettings = await services.Db.SiteSettings.FirstOrDefaultAsync(ct);
        if (siteSettings is null)
        {
            siteSettings = new SiteSettings
            {
                Id = Guid.NewGuid(),
                DefaultParser = Id.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            services.Db.SiteSettings.Add(siteSettings);
        }
        else
        {
            siteSettings.DefaultParser = Id.ToString();
            siteSettings.UpdatedAt = DateTime.UtcNow;
        }

        await services.Db.SaveChangesAsync(ct);
        return AiConfigurationMappings.MapToResponse(config, services.EncryptionService);
    }
}

// ── Shared mapping ────────────────────────────────────────────────────────────

file static class AiConfigurationMappings
{
    public static AiConfigurationResponse MapToResponse(AiConfiguration c,
        IApiKeyEncryptionService encryption)
    {
        return new AiConfigurationResponse
        {
            Id = c.Id,
            Name = c.Name,
            ProviderType = c.ProviderType,
            HasApiKey = c.EncryptedApiKey is not null,
            MaskedApiKey = encryption.Mask(c.EncryptedApiKey),
            BaseUrl = c.BaseUrl,
            DefaultModel = c.DefaultModel,
            IsActive = c.IsActive,
            EnableImageServices = c.EnableImageServices,
            EnableTranscriptionServices = c.EnableTranscriptionServices,
            CreatedAt = c.CreatedAt
        };
    }
}
