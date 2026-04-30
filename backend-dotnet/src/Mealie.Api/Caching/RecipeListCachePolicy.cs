using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.OutputCaching;

namespace Mealie.Api.Caching;

/// <summary>
/// Output cache policy for GET /api/recipes.
/// Caches per-household (vary by householdId + full query string), tags each entry
/// so it can be evicted when any recipe in that household changes.
/// </summary>
public sealed class RecipeListCachePolicy : IOutputCachePolicy
{
    public const string Name = "RecipeList";
    public static readonly RecipeListCachePolicy Instance = new();

    public static string TagFor(Guid householdId) => $"recipes-{householdId}";

    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken ct)
    {
        var tenant = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();

        context.EnableOutputCaching = true;
        context.AllowCacheLookup = true;
        context.AllowCacheStorage = true;
        context.AllowLocking = true;
        context.ResponseExpirationTimeSpan = TimeSpan.FromMinutes(5);

        // Vary by all filter/sort params the backend reads.
        context.CacheVaryByRules.QueryKeys = new[]
        {
            "page", "perPage", "search",
            "tags", "categories", "foods", "tools", "households",
            "requireAllCategories", "requireAllTags", "requireAllTools", "requireAllFoods",
            "orderBy", "orderDirection",
        };
        // Vary by household so different households never share a cache entry.
        context.CacheVaryByRules.VaryByValues.Add("householdId", tenant.HouseholdId.ToString());

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken ct)
        => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken ct)
    {
        var tenant = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
        context.Tags.Add(TagFor(tenant.HouseholdId));
        return ValueTask.CompletedTask;
    }
}
