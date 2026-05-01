using MediatR;

namespace Mealie.Domain.Events;

public record ShoppingListCreatedEvent(Guid ShoppingListId, Guid GroupId, Guid HouseholdId) : INotification;
