using MediatR;

namespace Mealie.Domain.Events;

public record RecipeCreatedEvent(Guid RecipeId, Guid HouseholdId, Guid? UserId = null) : INotification;
