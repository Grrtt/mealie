using MediatR;

namespace Mealie.Domain.Events;

public record RecipeDeletedEvent(Guid RecipeId) : INotification;
