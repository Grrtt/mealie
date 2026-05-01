using System.Net.Http.Json;
using Mealie.Domain.Entities.Settings;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Infrastructure.Webhooks;

/// <summary>
///     Handles domain events by dispatching to Apprise notifiers whose options match the event type.
/// </summary>
public class AppriseNotificationHandler(
    ApplicationDbContext db,
    HttpClient httpClient,
    ILogger<AppriseNotificationHandler> logger)
    : INotificationHandler<RecipeCreatedEvent>,
        INotificationHandler<RecipeUpdatedEvent>,
        INotificationHandler<RecipeDeletedEvent>,
        INotificationHandler<MealPlanEntryCreatedEvent>,
        INotificationHandler<MealPlanEntryUpdatedEvent>,
        INotificationHandler<MealPlanEntryDeletedEvent>,
        INotificationHandler<ShoppingListCreatedEvent>,
        INotificationHandler<ShoppingListUpdatedEvent>,
        INotificationHandler<ShoppingListDeletedEvent>,
        INotificationHandler<UserSignedUpEvent>
{
    public Task Handle(MealPlanEntryCreatedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "mealplan_entry_created",
            new { event_type = "mealplan_entry_created", mealplan_id = notification.MealPlanId }, ct,
            o => o.MealplanEntryCreated);
    }

    public Task Handle(MealPlanEntryDeletedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "mealplan_entry_deleted",
            new { event_type = "mealplan_entry_deleted", mealplan_id = notification.MealPlanId }, ct,
            o => o.MealplanEntryCreated);
    }

    public Task Handle(MealPlanEntryUpdatedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "mealplan_entry_updated",
            new { event_type = "mealplan_entry_updated", mealplan_id = notification.MealPlanId }, ct,
            o => o.MealplanEntryCreated);
    }

    public Task Handle(RecipeCreatedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "recipe_created",
            new { event_type = "recipe_created", recipe_id = notification.RecipeId }, ct,
            o => o.RecipeCreated);
    }

    public Task Handle(RecipeDeletedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "recipe_deleted",
            new { event_type = "recipe_deleted", recipe_id = notification.RecipeId }, ct,
            o => o.RecipeDeleted);
    }

    public Task Handle(RecipeUpdatedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "recipe_updated",
            new { event_type = "recipe_updated", recipe_id = notification.RecipeId }, ct,
            o => o.RecipeUpdated);
    }

    public Task Handle(ShoppingListCreatedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "shopping_list_created",
            new { event_type = "shopping_list_created", shopping_list_id = notification.ShoppingListId }, ct,
            o => o.ShoppingListCreated);
    }

    public Task Handle(ShoppingListDeletedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "shopping_list_deleted",
            new { event_type = "shopping_list_deleted", shopping_list_id = notification.ShoppingListId }, ct,
            o => o.ShoppingListDeleted);
    }

    public Task Handle(ShoppingListUpdatedEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "shopping_list_updated",
            new { event_type = "shopping_list_updated", shopping_list_id = notification.ShoppingListId }, ct,
            o => o.ShoppingListUpdated);
    }

    public Task Handle(UserSignedUpEvent notification, CancellationToken ct)
    {
        return DispatchAsync(notification.HouseholdId, "user_signup",
            new
            {
                event_type = "user_signup", user_id = notification.UserId, username = notification.Username,
                email = notification.Email
            }, ct,
            o => o.UserSignup);
    }

    private async Task DispatchAsync(
        Guid householdId,
        string eventType,
        object payload,
        CancellationToken ct,
        Func<EventNotifierOptions, bool> optionSelector)
    {
        var notifiers = await db.EventNotifiers.IgnoreQueryFilters()
            .Include(n => n.Options)
            .Where(n => n.HouseholdId == householdId && n.Enabled)
            .ToListAsync(ct);

        foreach (var notifier in notifiers)
        {
            if (notifier.Options is null || !optionSelector(notifier.Options))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(notifier.ApprisUrl))
            {
                continue;
            }

            // Only attempt delivery for HTTP/HTTPS URLs; other Apprise protocol URLs require the Apprise library
            if (!notifier.ApprisUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !notifier.ApprisUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Skipping non-HTTP Apprise URL for event {EventType}: {Url}", eventType,
                    notifier.ApprisUrl);
                continue;
            }

            try
            {
                var response = await httpClient.PostAsJsonAsync(notifier.ApprisUrl, payload, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Apprise delivery failed for {EventType}. Url={Url} Status={Status}", eventType,
                        notifier.ApprisUrl, (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Apprise delivery exception for {EventType}. Url={Url}", eventType,
                    notifier.ApprisUrl);
            }
        }
    }
}
