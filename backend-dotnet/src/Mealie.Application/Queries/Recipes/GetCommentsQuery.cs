using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record GetCommentsQuery(string Slug) : IQuery<IList<CommentResponse>>
{
    public async Task<IList<CommentResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null)
        {
            return [];
        }

        return await db.RecipeComments
            .Where(c => c.RecipeId == recipe.Id)
            .OrderBy(c => c.CreatedAt)
            .Select(c => CommentMappings.MapToResponse(c))
            .ToListAsync(ct);
    }
}

file static class CommentMappings
{
    public static CommentResponse MapToResponse(RecipeComment c)
    {
        return new CommentResponse
        {
            Id = c.Id, Text = c.Text, RecipeId = c.RecipeId, UserId = c.UserId,
            CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt
        };
    }
}
