using MediatR;

namespace Mealie.Domain.Events;

public record RecipeCreatedEvent(Guid RecipeId, Guid HouseholdId) : INotification;
