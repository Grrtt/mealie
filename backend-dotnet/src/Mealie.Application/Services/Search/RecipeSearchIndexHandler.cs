using Mealie.Application.Contracts;
using Mealie.Domain.Events;
using MediatR;

namespace Mealie.Application.Services.Search;

public class RecipeSearchIndexHandler(IRecipeSearchIndex index) :
    INotificationHandler<RecipeCreatedEvent>,
    INotificationHandler<RecipeUpdatedEvent>,
    INotificationHandler<RecipeDeletedEvent>,
    INotificationHandler<RecipesBulkDeletedEvent>
{
    public Task Handle(RecipeCreatedEvent n, CancellationToken ct) =>
        index.IndexRecipeAsync(n.RecipeId, ct);

    public Task Handle(RecipeUpdatedEvent n, CancellationToken ct) =>
        index.IndexRecipeAsync(n.RecipeId, ct);

    public Task Handle(RecipeDeletedEvent n, CancellationToken ct) =>
        index.RemoveRecipeAsync(n.RecipeId, ct);

    public async Task Handle(RecipesBulkDeletedEvent n, CancellationToken ct)
    {
        foreach (var id in n.RecipeIds)
            await index.RemoveRecipeAsync(id, ct);
    }
}
