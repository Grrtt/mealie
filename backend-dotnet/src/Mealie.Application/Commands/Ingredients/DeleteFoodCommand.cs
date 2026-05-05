using Mealie.Application.Queries;
using Mealie.Application.Services.Ingredients;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record DeleteFoodCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await IngredientCrudCore.DeleteFoodAsync(services.Db, services.Mediator, GroupId, Id, ct);
    }
}
