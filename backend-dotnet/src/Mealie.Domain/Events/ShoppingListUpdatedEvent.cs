using MediatR;

namespace Mealie.Domain.Events;

public record ShoppingListUpdatedEvent(Guid ShoppingListId, Guid GroupId, Guid HouseholdId) : INotification;
