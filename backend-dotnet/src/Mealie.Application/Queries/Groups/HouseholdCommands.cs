using Mealie.Application.Common;
using Mealie.Application.Dtos.Groups;
using Mealie.Domain.Entities.Core;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Groups;

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

public record DeleteHouseholdCommand(Guid HouseholdId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var h = await db.Households.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == HouseholdId, ct);
        if (h is null) return false;
        db.Households.Remove(h);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

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

public record CreateHouseholdInviteTokenCommand(Guid GroupId, Guid HouseholdId, CreateInviteTokenRequest Request)
    : IQuery<InviteTokenResponse>
{
    public async Task<InviteTokenResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var token = new GroupInviteToken
        {
            Id = Guid.NewGuid(), Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            GroupId = GroupId, HouseholdId = HouseholdId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.InviteTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return new InviteTokenResponse { Id = token.Id, Token = token.Token, GroupId = GroupId, HouseholdId = HouseholdId };
    }
}

public record UpdateHouseholdMemberPermissionsCommand(Guid HouseholdId, Guid UserId, bool Admin, bool CanOrganize, bool CanInvite)
    : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == UserId && u.HouseholdId == HouseholdId, ct);
        if (user is null) return false;
        user.Admin = Admin;
        user.CanOrganize = CanOrganize;
        user.CanInvite = CanInvite;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

file static class HouseholdMappings
{
    public static HouseholdResponse MapToResponse(Household h) =>
        new() { Id = h.Id, Name = h.Name, Slug = h.Slug, GroupId = h.GroupId };

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
