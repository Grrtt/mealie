using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Recipes;

public record GetCommentByIdQuery(Guid CommentId) : IQuery<CommentResponse?>
{
    public async Task<CommentResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var comment = await services.Db.RecipeComments.FirstOrDefaultAsync(c => c.Id == CommentId, ct);
        return comment is null ? null : CommentMappings.MapToResponse(comment);
    }
}

file static class CommentMappings
{
    public static CommentResponse MapToResponse(RecipeComment c) => new()
    {
        Id = c.Id, Text = c.Text, RecipeId = c.RecipeId, UserId = c.UserId,
        CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt
    };
}
