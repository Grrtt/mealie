using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record CreateShareTokenCommand(string Slug, Guid GroupId, CreateShareTokenRequest Request) : IQuery<ShareTokenResponse?>
{
    public async Task<ShareTokenResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null) return null;
        var token = new RecipeShareToken
        {
            Id = Guid.NewGuid(), RecipeId = recipe.Id, GroupId = GroupId,
            CreatedAt = DateTime.UtcNow, ExpiresAt = Request.ExpiresAt
        };
        db.RecipeShareTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return ShareMappings.MapToResponse(token);
    }
}

public record DeleteShareTokenCommand(Guid TokenId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var token = await services.Db.RecipeShareTokens.FindAsync([TokenId], ct);
        if (token is null) return false;
        services.Db.RecipeShareTokens.Remove(token);
        await services.Db.SaveChangesAsync(ct);
        return true;
    }
}

file static class ShareMappings
{
    public static ShareTokenResponse MapToResponse(RecipeShareToken t) => new()
    {
        Id = t.Id, RecipeId = t.RecipeId, GroupId = t.GroupId, CreatedAt = t.CreatedAt, ExpiresAt = t.ExpiresAt
    };
}
