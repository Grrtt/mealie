using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries.Shared;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetRecipesByCategoryQuery(Guid GroupId, Guid CategoryId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
    {
        var category = await services.Db.Categories.IgnoreQueryFilters()
            .Include(c => c.Recipes).ThenInclude(r => r.Tags)
            .Include(c => c.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(c => c.GroupId == GroupId && c.Id == CategoryId, ct);
        if (category is null)
        {
            return [];
        }

        return category.Recipes.Select(RecipeMappings.MapToSummary).ToList();
    }
}
