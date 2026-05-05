using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Queries.Organizers;

public record GetRecipesByCategoryQuery(Guid GroupId, Guid CategoryId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetRecipesByCategoryAsync(services.Db, GroupId, CategoryId, ct);
    }
}
