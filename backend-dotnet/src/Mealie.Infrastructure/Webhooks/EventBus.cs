using Microsoft.Extensions.Logging;

namespace Mealie.Infrastructure.Webhooks;

public interface IEventBus
{
    void Publish<T>(T eventData) where T : class;
    void Subscribe<T>(Func<T, Task> handler) where T : class;
}

public class EventBus(ILogger<EventBus> logger) : IEventBus
{
    private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = [];

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type)) _handlers[type] = [];
        _handlers[type].Add(async obj => await handler((T)obj));
    }

    public void Publish<T>(T eventData) where T : class
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var handlers)) return;

        // Fire and forget
        _ = Task.Run(async () =>
        {
            foreach (var handler in handlers)
            {
                try { await handler(eventData!); }
                catch (Exception ex) { logger.LogWarning(ex, "Event handler failed for {EventType}", type.Name); }
            }
        });
    }
}

// Domain events
public record RecipeCreatedEvent(Guid RecipeId, string Slug, Guid GroupId, Guid HouseholdId);
public record RecipeUpdatedEvent(Guid RecipeId, string Slug, Guid GroupId, Guid HouseholdId);
public record RecipeDeletedEvent(Guid RecipeId, string Slug, Guid GroupId, Guid HouseholdId);
public record MealPlanCreatedEvent(Guid MealPlanId, Guid GroupId, Guid HouseholdId);
public record ShoppingListCreatedEvent(Guid ShoppingListId, Guid GroupId, Guid HouseholdId);
