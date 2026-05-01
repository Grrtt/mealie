using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Recipes;

public class RecipeShareService(ApplicationDbContext db) : IRecipeShareService
{
    public async Task<IList<ShareTokenResponse>> GetShareTokensAsync(string slug, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (recipe is null)
        {
            return [];
        }

        return await db.RecipeShareTokens
            .Where(t => t.RecipeId == recipe.Id)
            .Select(t => new ShareTokenResponse
            {
                Id = t.Id,
                RecipeId = t.RecipeId,
                GroupId = t.GroupId,
                CreatedAt = t.CreatedAt,
                ExpiresAt = t.ExpiresAt
            })
            .ToListAsync(ct);
    }

    public async Task<ShareTokenResponse?> CreateShareTokenAsync(string slug, Guid groupId,
        CreateShareTokenRequest request, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (recipe is null)
        {
            return null;
        }

        var token = new RecipeShareToken
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt
        };

        db.RecipeShareTokens.Add(token);
        await db.SaveChangesAsync(ct);

        return new ShareTokenResponse
        {
            Id = token.Id,
            RecipeId = token.RecipeId,
            GroupId = token.GroupId,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt
        };
    }

    public async Task<bool> DeleteShareTokenAsync(Guid tokenId, CancellationToken ct = default)
    {
        var token = await db.RecipeShareTokens.FindAsync([tokenId], ct);
        if (token is null)
        {
            return false;
        }

        db.RecipeShareTokens.Remove(token);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ShareTokenResponse?> GetShareTokenAsync(Guid tokenId, CancellationToken ct = default)
    {
        var token = await db.RecipeShareTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tokenId && (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow), ct);
        if (token is null)
        {
            return null;
        }

        return new ShareTokenResponse
        {
            Id = token.Id,
            RecipeId = token.RecipeId,
            GroupId = token.GroupId,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt
        };
    }
}
