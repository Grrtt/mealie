using MediatR;
namespace Mealie.Domain.Events;
public record FoodUpdatedEvent(Guid FoodId, Guid GroupId) : INotification;
