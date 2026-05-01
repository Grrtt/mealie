using System.Text.RegularExpressions;
using Mealie.Application.Dtos.Admin;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Admin;

public record CreateAdminHouseholdCommand(CreateAdminHouseholdRequest Request) : IQuery<AdminHouseholdResponse>
{
    public async Task<AdminHouseholdResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = await AdminHouseholdMappings.GenerateUniqueHouseholdSlugAsync(db, Request.Name, ct);
        var household = new Household
        {
            Id = Guid.NewGuid(), Name = Request.Name, Slug = slug, GroupId = Request.GroupId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Households.Add(household);
        await db.SaveChangesAsync(ct);
        return AdminHouseholdMappings.MapHouseholdToResponse(household);
    }
}

file static class AdminHouseholdMappings
{
    public static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    public static async Task<string> GenerateUniqueHouseholdSlugAsync(Mealie.Infrastructure.Data.ApplicationDbContext db, string name, CancellationToken ct)
    {
        var baseSlug = GenerateSlug(name);
        var slug = baseSlug;
        var i = 1;
        while (await db.Households.IgnoreQueryFilters().AnyAsync(h => h.Slug == slug, ct))
            slug = $"{baseSlug}-{i++}";
        return slug;
    }

    public static Mealie.Application.Dtos.Admin.AdminHouseholdResponse MapHouseholdToResponse(Mealie.Domain.Entities.Core.Household h) =>
        new()
        {
            Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId,
            CreatedAt = h.CreatedAt, UpdateAt = h.UpdateAt,
            UserCount = h.Users.Count,
            Users = h.Users.Select(u => (object)new { u.Id, u.FullName, u.Username, u.Email }).ToList(),
            Webhooks = [],
            Preferences = h.Preferences is null ? null : new Mealie.Application.Dtos.Admin.HouseholdPreferencesDto
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