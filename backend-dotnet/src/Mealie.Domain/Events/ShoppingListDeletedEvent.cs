using MediatR;

namespace Mealie.Domain.Events;

public record ShoppingListDeletedEvent(Guid ShoppingListId, Guid HouseholdId) : INotification;
