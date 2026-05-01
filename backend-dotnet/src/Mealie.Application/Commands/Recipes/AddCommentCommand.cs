using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record AddCommentCommand(string Slug, Guid UserId, CreateCommentRequest Request) : IQuery<CommentResponse?>
{
    public async Task<CommentResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (recipe is null) return null;
        var comment = new RecipeComment
        {
            Id = Guid.NewGuid(), Text = Request.Text, RecipeId = recipe.Id, UserId = UserId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.RecipeComments.Add(comment);
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