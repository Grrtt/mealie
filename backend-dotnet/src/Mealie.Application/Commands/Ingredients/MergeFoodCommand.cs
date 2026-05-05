using Mealie.Application.Queries;
using Mealie.Application.Services.Ingredients;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record MergeFoodCommand(Guid GroupId, Guid FromFoodId, Guid ToFoodId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await IngredientCrudCore.MergeFoodAsync(services.Db, services.Mediator, GroupId, FromFoodId, ToFoodId, ct);
    }
}
