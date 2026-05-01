using Mealie.Application.Dtos.Admin;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Admin;

public record GetAdminHouseholdQuery(Guid HouseholdId) : IQuery<AdminHouseholdResponse?>
{
    public async Task<AdminHouseholdResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var h = await services.Db.Households.IgnoreQueryFilters()
            .Include(h => h.Users).Include(h => h.Preferences)
            .FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        return h is null ? null : AdminHouseholdMappings.MapHouseholdToResponse(h);
    }
}

file static class AdminHouseholdMappings
{
    public static AdminHouseholdResponse MapHouseholdToResponse(Household h) =>
        new()
        {
            Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId,
            CreatedAt = h.CreatedAt, UpdateAt = h.UpdateAt,
            UserCount = h.Users.Count,
            Users = h.Users.Select(u => (object)new { u.Id, u.FullName, u.Username, u.Email }).ToList(),
            Webhooks = [],
            Preferences = h.Preferences is null ? null : new HouseholdPreferencesDto
            {
                Id = h.Preferences.Id, HouseholdId = h.Preferences.HouseholdId,
                PrivateHousehold = h.Preferences.PrivateHousehold, ShowAnnouncements = false,
                RecipePublic = bool.TryParse(h.Preferences.RecipePublic, out var rp) && rp,
                RecipeShowNutrition = bool.TryParse(h.Preferences.RecipeShowNutrition, out var rsn) && rsn,
                RecipeShowAssets = bool.TryParse(h.Preferences.RecipeShowAssets, out var rsa) && rsa,
                RecipeLandscapeView = bool.TryParse(h.Preferences.RecipeLandscapeView, out var rlv) && rlv,
                RecipeDisableComments = bool.TryParse(h.Preferences.RecipeDisableComments, out var rdc) && rdc,
                RecipeDisableAmount = bool.TryParse(h.Preferences.RecipeDisableAmount, out var rda) && rda
            }
        };
}
