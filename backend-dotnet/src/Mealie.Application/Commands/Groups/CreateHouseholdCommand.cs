using Mealie.Application.Common;
using Mealie.Application.Dtos.Groups;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Groups;

public record CreateHouseholdCommand(Guid GroupId, CreateHouseholdRequest Request) : IQuery<HouseholdResponse>
{
    public async Task<HouseholdResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = SlugHelper.Generate(Request.Name);
        var household = new Household
        {
            Id = Guid.NewGuid(), Name = Request.Name, Slug = slug, GroupId = GroupId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Households.Add(household);
        await db.SaveChangesAsync(ct);
        return HouseholdMappings.MapToResponse(household);
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