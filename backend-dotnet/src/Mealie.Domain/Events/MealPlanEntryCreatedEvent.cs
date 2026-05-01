using MediatR;

namespace Mealie.Domain.Events;

public record MealPlanEntryCreatedEvent(Guid MealPlanId, Guid GroupId, Guid HouseholdId) : INotification;
