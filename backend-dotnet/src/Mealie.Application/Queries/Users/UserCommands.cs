using Mealie.Application.Dtos.Users;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Users;

public record UpdateUserProfileCommand(Guid UserId, UpdateUserRequest Request) : IQuery<UserResponse?>
{
    public async Task<UserResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.Group).Include(u => u.Household)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return null;
        if (Request.FullName is not null) user.FullName = Request.FullName;
        if (Request.Email is not null) user.Email = Request.Email;
        if (Request.Username is not null) user.Username = Request.Username;
        user.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return UserMappings.MapToResponse(user);
    }
}

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null || user.Password is null) return false;
        if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, user.Password)) return false;
        user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
        user.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record CreateApiKeyCommand(Guid UserId, string Name) : IQuery<ApiKeyResponse>
{
    public async Task<ApiKeyResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var hash = BCrypt.Net.BCrypt.HashPassword(rawToken);
        var apiKey = new ApiKey
        {
            Name = Name, Token = hash, UserId = UserId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);
        return new ApiKeyResponse { Id = apiKey.Id, Name = Name, Token = rawToken };
    }
}

public record DeleteApiKeyCommand(Guid UserId, int KeyId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == KeyId && k.UserId == UserId, ct);
        if (key is null) return false;
        db.ApiKeys.Remove(key);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record AddFavoriteRecipeCommand(Guid UserId, string Slug) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        var recipe = await db.Recipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (user is null || recipe is null) return false;
        if (!user.FavoriteRecipes.Any(r => r.Id == recipe.Id))
        {
            user.FavoriteRecipes.Add(recipe);
            await db.SaveChangesAsync(ct);
        }
        return true;
    }
}

public record RemoveFavoriteRecipeCommand(Guid UserId, string Slug) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        var recipe = user?.FavoriteRecipes.FirstOrDefault(r => r.Slug == Slug);
        if (recipe is null) return false;
        user!.FavoriteRecipes.Remove(recipe);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record SetUserRatingCommand(Guid UserId, string Slug, int? Rating, bool? IsFavorite) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        if (IsFavorite == true)
            await new AddFavoriteRecipeCommand(UserId, Slug).ExecuteAsync(services, ct);
        else if (IsFavorite == false)
            await new RemoveFavoriteRecipeCommand(UserId, Slug).ExecuteAsync(services, ct);

        if (Rating.HasValue)
        {
            var recipe = await db.Recipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Slug == Slug, ct);
            if (recipe is not null)
            {
                recipe.Rating = Rating;
                await db.SaveChangesAsync(ct);
            }
        }
        return true;
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
