using Mealie.Application.Common;
using Mealie.Application.Dtos.Groups;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Groups;

public record UpdateHouseholdCommand(Guid HouseholdId, UpdateHouseholdRequest Request) : IQuery<HouseholdResponse?>
{
    public async Task<HouseholdResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        if (h is null) return null;
        h.Name = Request.Name;
        h.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return HouseholdMappings.MapToResponse(h);
    }
}

file static class HouseholdMappings
{
    public static Mealie.Application.Dtos.Groups.HouseholdResponse MapToResponse(Mealie.Domain.Entities.Core.Household h) =>
        new() { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId };

    public static Mealie.Application.Dtos.Groups.HouseholdPreferencesResponse MapToHouseholdPreferencesResponse(Mealie.Domain.Entities.Settings.HouseholdPreferences prefs) =>
        new()
        {
            Id = prefs.Id, HouseholdId = prefs.HouseholdId, PrivateHousehold = prefs.PrivateHousehold,
            FirstDayOfWeek = prefs.FirstDayOfWeek, RecipePublic = prefs.RecipePublic,
            RecipeShowNutrition = prefs.RecipeShowNutrition, RecipeShowAssets = prefs.RecipeShowAssets,
            RecipeLandscapeView = prefs.RecipeLandscapeView, RecipeDisableComments = prefs.RecipeDisableComments,
            RecipeDisableAmount = prefs.RecipeDisableAmount
        };
}