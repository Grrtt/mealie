using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record GetAllCommentsQuery(Guid GroupId) : IQuery<IList<CommentResponse>>
{
    public async Task<IList<CommentResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.RecipeComments
            .Include(c => c.Recipe)
            .Where(c => c.Recipe.GroupId == GroupId)
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
