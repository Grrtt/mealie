using Mealie.Application.Queries;
using Mealie.Application.Services.Ingredients;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record MergeUnitCommand(Guid GroupId, Guid FromUnitId, Guid ToUnitId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await IngredientCrudCore.MergeUnitAsync(services.Db, GroupId, FromUnitId, ToUnitId, ct);
    }
}
