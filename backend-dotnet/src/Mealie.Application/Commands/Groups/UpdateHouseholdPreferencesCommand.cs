using Mealie.Application.Common;
using Mealie.Application.Dtos.Groups;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Groups;

public record UpdateHouseholdPreferencesCommand(Guid HouseholdId, UpdateHouseholdPreferencesRequest Request)
    : IQuery<HouseholdPreferencesResponse?>
{
    public async Task<HouseholdPreferencesResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var prefs = await db.HouseholdPreferences.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.HouseholdId == HouseholdId, ct);
        if (prefs is null) return null;

        if (Request.PrivateHousehold.HasValue) prefs.PrivateHousehold = Request.PrivateHousehold.Value;
        if (Request.FirstDayOfWeek is not null) prefs.FirstDayOfWeek = Request.FirstDayOfWeek;
        if (Request.RecipePublic is not null) prefs.RecipePublic = Request.RecipePublic;
        if (Request.RecipeShowNutrition is not null) prefs.RecipeShowNutrition = Request.RecipeShowNutrition;
        if (Request.RecipeShowAssets is not null) prefs.RecipeShowAssets = Request.RecipeShowAssets;
        if (Request.RecipeLandscapeView is not null) prefs.RecipeLandscapeView = Request.RecipeLandscapeView;
        if (Request.RecipeDisableComments is not null) prefs.RecipeDisableComments = Request.RecipeDisableComments;
        if (Request.RecipeDisableAmount is not null) prefs.RecipeDisableAmount = Request.RecipeDisableAmount;

        prefs.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return HouseholdMappings.MapToHouseholdPreferencesResponse(prefs);
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