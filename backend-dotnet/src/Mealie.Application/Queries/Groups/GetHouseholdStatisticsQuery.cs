using Mealie.Application.Dtos.Groups;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetHouseholdStatisticsQuery(Guid HouseholdId) : IQuery<HouseholdStatisticsResponse>
{
    public async Task<HouseholdStatisticsResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var totalRecipes = await db.Recipes.IgnoreQueryFilters().CountAsync(r => r.HouseholdId == HouseholdId, ct);
        var totalUsers = await db.Users.IgnoreQueryFilters().CountAsync(u => u.HouseholdId == HouseholdId, ct);
        var household = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);

        int totalCategories = 0, totalTags = 0, totalTools = 0;
        if (household is not null)
        {
            totalCategories = await db.Categories.IgnoreQueryFilters().CountAsync(c => c.GroupId == household.GroupId, ct);
            totalTags = await db.Tags.IgnoreQueryFilters().CountAsync(t => t.GroupId == household.GroupId, ct);
            totalTools = await db.Tools.IgnoreQueryFilters().CountAsync(t => t.GroupId == household.GroupId, ct);
        }

        return new HouseholdStatisticsResponse
        {
            TotalRecipes = totalRecipes, TotalUsers = totalUsers,
            TotalCategories = totalCategories, TotalTags = totalTags, TotalTools = totalTools
        };
    }
}
