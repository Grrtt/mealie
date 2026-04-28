using MediatR;

namespace Mealie.Domain.Events;

public record RecipesBulkDeletedEvent(IList<Guid> RecipeIds) : INotification;
