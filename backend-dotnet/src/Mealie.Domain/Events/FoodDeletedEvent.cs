using MediatR;

namespace Mealie.Domain.Events;

public record FoodDeletedEvent(Guid FoodId) : INotification;
