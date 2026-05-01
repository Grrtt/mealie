using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record UpdateCommentCommand(Guid CommentId, Guid UserId, UpdateCommentRequest Request) : IQuery<CommentResponse?>
{
    public async Task<CommentResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var comment = await db.RecipeComments.FirstOrDefaultAsync(c => c.Id == CommentId && c.UserId == UserId, ct);
        if (comment is null) return null;
        comment.Text = Request.Text;
        comment.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return CommentMappings.MapToResponse(comment);
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