using MediatR;

namespace Mealie.Domain.Events;

public record FoodCreatedEvent(Guid FoodId, Guid GroupId) : INotification;
