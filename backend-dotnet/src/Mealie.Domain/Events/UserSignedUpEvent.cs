using MediatR;

namespace Mealie.Domain.Events;

public record UserSignedUpEvent(Guid UserId, string Username, string Email, Guid GroupId, Guid HouseholdId) : INotification;
