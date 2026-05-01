using Mealie.Application.Dtos.Users;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Users;

public record GetUserProfileQuery(Guid UserId) : IQuery<UserResponse?>
{
    public async Task<UserResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var user = await services.Db.Users.IgnoreQueryFilters()
            .Include(u => u.Group).Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        return user is null ? null : UserMappings.MapToResponse(user);
    }
}

public record GetApiKeysQuery(Guid UserId) : IQuery<IList<ApiKeyResponse>>
{
    public async Task<IList<ApiKeyResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.ApiKeys
            .Where(k => k.UserId == UserId)
            .Select(k => new ApiKeyResponse { Id = k.Id, Name = k.Name })
            .ToListAsync(ct);
    }
}

public record GetFavoriteRecipesQuery(Guid UserId) : IQuery<IList<string>>
{
    public async Task<IList<string>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var user = await services.Db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        return user is null ? [] : user.FavoriteRecipes.Select(r => r.Slug).ToList();
    }
}

public record GetUserRatingsQuery(Guid UserId) : IQuery<IList<UserRatingResponse>>
{
    public async Task<IList<UserRatingResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var user = await services.Db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return [];
        return user.FavoriteRecipes.Select(r => new UserRatingResponse
        {
            Id = r.Id, RecipeId = r.Id, Slug = r.Slug, IsFavorite = true, Rating = r.Rating
        }).ToList();
    }
}

file static class UserMappings
{
    public static UserResponse MapToResponse(User u) =>
        new()
        {
            Id = u.Id, FullName = u.FullName, Username = u.Username, Email = u.Email,
            AuthMethod = u.AuthMethod.ToString(), Admin = u.Admin, Advanced = u.Advanced,
            GroupId = u.GroupId, Group = u.Group?.Name ?? string.Empty,
            GroupSlug = u.Group?.Slug ?? string.Empty,
            HouseholdId = u.HouseholdId, Household = u.Household?.Name ?? string.Empty,
            HouseholdSlug = u.Household?.Slug ?? string.Empty,
            CanManageHousehold = u.CanManageHousehold, CanManage = u.CanManage,
            CanInvite = u.CanInvite, CanOrganize = u.CanOrganize
        };
}
