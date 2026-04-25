using Mealie.Application.Dtos.Users;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.Users;

public class UserService(ApplicationDbContext db, ILogger<UserService> logger) : IUserService
{
    public async Task<UserResponse?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;
        return MapToResponse(user);
    }

    public async Task<UserResponse?> UpdateProfileAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;
        if (request.FullName is not null) user.FullName = request.FullName;
        if (request.Email is not null) user.Email = request.Email;
        if (request.Username is not null) user.Username = request.Username;
        user.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(user);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || user.Password is null) return false;
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password)) return false;
        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ApiKeyResponse> CreateApiKeyAsync(Guid userId, string name, CancellationToken ct = default)
    {
        var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var hash = BCrypt.Net.BCrypt.HashPassword(rawToken);
        var apiKey = new Domain.Entities.Core.ApiKey
        {
            Name = name,
            Token = hash,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);
        return new ApiKeyResponse { Id = apiKey.Id, Name = name, Token = rawToken };
    }

    public async Task<IList<ApiKeyResponse>> GetApiKeysAsync(Guid userId, CancellationToken ct = default)
    {
        return await db.ApiKeys
            .Where(k => k.UserId == userId)
            .Select(k => new ApiKeyResponse { Id = k.Id, Name = k.Name })
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteApiKeyAsync(Guid userId, int keyId, CancellationToken ct = default)
    {
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId && k.UserId == userId, ct);
        if (key is null) return false;
        db.ApiKeys.Remove(key);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task AddFavoriteAsync(Guid userId, string slug, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        var recipe = await db.Recipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (user is null || recipe is null) return;
        if (!user.FavoriteRecipes.Any(r => r.Id == recipe.Id))
        {
            user.FavoriteRecipes.Add(recipe);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveFavoriteAsync(Guid userId, string slug, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        var recipe = user?.FavoriteRecipes.FirstOrDefault(r => r.Slug == slug);
        if (recipe is not null)
        {
            user!.FavoriteRecipes.Remove(recipe);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IList<UserRatingResponse>> GetRatingsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return [];

        return user.FavoriteRecipes.Select(r => new UserRatingResponse
        {
            Id = r.Id,
            RecipeId = r.Id,
            Slug = r.Slug,
            IsFavorite = true,
            Rating = r.Rating
        }).ToList();
    }

    public async Task SetRatingAsync(Guid userId, string slug, int? rating, bool? isFavorite, CancellationToken ct = default)
    {
        if (isFavorite == true) await AddFavoriteAsync(userId, slug, ct);
        else if (isFavorite == false) await RemoveFavoriteAsync(userId, slug, ct);

        if (rating.HasValue)
        {
            var recipe = await db.Recipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Slug == slug, ct);
            if (recipe is not null)
            {
                recipe.Rating = rating;
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static UserResponse MapToResponse(Domain.Entities.Core.User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Username = user.Username,
        Email = user.Email,
        AuthMethod = user.AuthMethod.ToString(),
        Admin = user.Admin,
        Advanced = user.Advanced,
        GroupId = user.GroupId,
        HouseholdId = user.HouseholdId,
        CanManageHousehold = user.CanManageHousehold,
        CanManage = user.CanManage,
        CanInvite = user.CanInvite,
        CanOrganize = user.CanOrganize
    };
}
