using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries.Shared;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Organizers;

public record GetRecipesByToolQuery(Guid GroupId, Guid ToolId) : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var tool = await services.Db.Tools.IgnoreQueryFilters()
            .Include(t => t.Recipes).ThenInclude(r => r.Tags)
            .Include(t => t.Recipes).ThenInclude(r => r.Categories)
            .FirstOrDefaultAsync(t => t.GroupId == GroupId && t.Id == ToolId, ct);
        if (tool is null) return [];
        return tool.Recipes.Select(RecipeMappings.MapToSummary).ToList();
    }
}
