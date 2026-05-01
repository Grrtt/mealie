using Mealie.Application.Contracts.Search;
using Mealie.Domain.Events;
using MediatR;

namespace Mealie.Application.Services.Search;

public class RecipeSearchIndexHandler(IRecipeSearchIndex index) :
    INotificationHandler<RecipeCreatedEvent>,
    INotificationHandler<RecipeUpdatedEvent>,
    INotificationHandler<RecipeDeletedEvent>,
    INotificationHandler<RecipesBulkDeletedEvent>
{
    public Task Handle(RecipeCreatedEvent n, CancellationToken ct)
    {
        return index.IndexAsync(n.RecipeId, ct);
    }

    public Task Handle(RecipeDeletedEvent n, CancellationToken ct)
    {
        return index.RemoveAsync(n.RecipeId, ct);
    }

    public async Task Handle(RecipesBulkDeletedEvent n, CancellationToken ct)
    {
        foreach (var id in n.RecipeIds)
        {
            await index.RemoveAsync(id, ct);
        }
    }

    public Task Handle(RecipeUpdatedEvent n, CancellationToken ct)
    {
        return index.IndexAsync(n.RecipeId, ct);
    }
}
