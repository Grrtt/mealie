using MediatR;

namespace Mealie.Domain.Events;

public record RecipeUpdatedEvent(Guid RecipeId, Guid HouseholdId) : INotification;
