using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record GetShareTokenQuery(Guid TokenId) : IQuery<ShareTokenResponse?>
{
    public async Task<ShareTokenResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var token = await services.Db.RecipeShareTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == TokenId && (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow), ct);
        return token is null ? null : ShareMappings.MapToResponse(token);
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
