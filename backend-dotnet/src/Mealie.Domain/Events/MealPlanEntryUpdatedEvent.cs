using MediatR;

namespace Mealie.Domain.Events;

public record MealPlanEntryUpdatedEvent(Guid MealPlanId, Guid GroupId, Guid HouseholdId) : INotification;
