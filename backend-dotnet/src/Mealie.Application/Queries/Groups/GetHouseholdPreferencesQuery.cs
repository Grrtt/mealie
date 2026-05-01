using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

public record GetHouseholdPreferencesQuery(Guid HouseholdId) : IQuery<HouseholdPreferencesResponse?>
{
    public async Task<HouseholdPreferencesResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var prefs = await services.Db.HouseholdPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.HouseholdId == HouseholdId, ct);
        return prefs is null ? null : HouseholdMappings.MapToHouseholdPreferencesResponse(prefs);
    }
}

file static class HouseholdMappings
{
    public static HouseholdPreferencesResponse MapToHouseholdPreferencesResponse(HouseholdPreferences prefs) =>
        new()
        {
            Id = prefs.Id, HouseholdId = prefs.HouseholdId, PrivateHousehold = prefs.PrivateHousehold,
            FirstDayOfWeek = prefs.FirstDayOfWeek, RecipePublic = prefs.RecipePublic,
            RecipeShowNutrition = prefs.RecipeShowNutrition, RecipeShowAssets = prefs.RecipeShowAssets,
            RecipeLandscapeView = prefs.RecipeLandscapeView, RecipeDisableComments = prefs.RecipeDisableComments,
            RecipeDisableAmount = prefs.RecipeDisableAmount
        };
}
