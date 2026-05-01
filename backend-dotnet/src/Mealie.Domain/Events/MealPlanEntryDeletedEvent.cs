using MediatR;

namespace Mealie.Domain.Events;

public record MealPlanEntryDeletedEvent(Guid MealPlanId, Guid HouseholdId) : INotification;
