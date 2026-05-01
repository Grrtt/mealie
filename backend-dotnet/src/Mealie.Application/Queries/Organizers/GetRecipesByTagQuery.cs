using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries.Shared;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetRecipesByTagQuery(Guid GroupId, Guid TagId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var tag = await services.Db.Tags.IgnoreQueryFilters()
            .Include(t => t.Recipes).ThenInclude(r => r.Tags)
            .Include(t => t.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == TagId, ct);
        if (tag is null) return [];
        return tag.Recipes.Select(RecipeMappings.MapToSummary).ToList();
    }
}
