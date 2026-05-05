using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Queries.Organizers;

public record GetRecipesByTagQuery(Guid GroupId, Guid TagId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetRecipesByTagAsync(services.Db, GroupId, TagId, ct);
    }
}
