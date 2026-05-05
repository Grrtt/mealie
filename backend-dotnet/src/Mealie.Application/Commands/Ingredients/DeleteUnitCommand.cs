using Mealie.Application.Queries;
using Mealie.Application.Services.Ingredients;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record DeleteUnitCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await IngredientCrudCore.DeleteUnitAsync(services.Db, GroupId, Id, ct);
    }
}
