using Mealie.Application.Contracts.Search;
using Mealie.Domain.Events;
using MediatR;

namespace Mealie.Application.Services.Search;

public class FoodSearchIndexHandler(IFoodSearchIndex index) :
    INotificationHandler<FoodCreatedEvent>,
    INotificationHandler<FoodUpdatedEvent>,
    INotificationHandler<FoodDeletedEvent>
{
    public Task Handle(FoodCreatedEvent n, CancellationToken ct)
    {
        return index.IndexAsync(n.FoodId, ct);
    }

    public Task Handle(FoodDeletedEvent n, CancellationToken ct)
    {
        return index.RemoveAsync(n.FoodId, ct);
    }

    public Task Handle(FoodUpdatedEvent n, CancellationToken ct)
    {
        return index.IndexAsync(n.FoodId, ct);
    }
}
