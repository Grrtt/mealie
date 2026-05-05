using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Queries.Organizers;

public record GetRecipesByToolQuery(Guid GroupId, Guid ToolId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetRecipesByToolAsync(services.Db, GroupId, ToolId, ct);
    }
}
