using Mealie.Domain.Events;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;

namespace Mealie.Api.Caching;

/// <summary>
///     Evicts the recipe list output cache for the affected household whenever a recipe
///     is created, updated, or deleted.
/// </summary>
public sealed class RecipeCacheInvalidationHandler(IOutputCacheStore cacheStore)
    : INotificationHandler<RecipeCreatedEvent>,
        INotificationHandler<RecipeUpdatedEvent>,
        INotificationHandler<RecipeDeletedEvent>
{
    public async Task Handle(RecipeCreatedEvent n, CancellationToken ct)
    {
        await cacheStore.EvictByTagAsync(RecipeListCachePolicy.TagFor(n.HouseholdId), ct);
    }

    public async Task Handle(RecipeDeletedEvent n, CancellationToken ct)
    {
        await cacheStore.EvictByTagAsync(RecipeListCachePolicy.TagFor(n.HouseholdId), ct);
    }

    public async Task Handle(RecipeUpdatedEvent n, CancellationToken ct)
    {
        await cacheStore.EvictByTagAsync(RecipeListCachePolicy.TagFor(n.HouseholdId), ct);
    }
}
