using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record GetShareTokensQuery(string Slug) : IQuery<IList<ShareTokenResponse>>
{
    public async Task<IList<ShareTokenResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null)
        {
            return [];
        }

        return await db.RecipeShareTokens
            .Where(t => t.RecipeId == recipe.Id)
            .Select(t => ShareMappings.MapToResponse(t))
            .ToListAsync(ct);
    }
}

file static class ShareMappings
{
    public static ShareTokenResponse MapToResponse(RecipeShareToken t)
    {
        return new ShareTokenResponse
        {
            Id = t.Id, RecipeId = t.RecipeId, GroupId = t.GroupId, CreatedAt = t.CreatedAt, ExpiresAt = t.ExpiresAt
        };
    }
}
