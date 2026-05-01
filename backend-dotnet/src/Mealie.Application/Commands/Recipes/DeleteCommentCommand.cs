using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Recipes;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record DeleteCommentCommand(Guid CommentId, Guid UserId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var comment = await db.RecipeComments.FirstOrDefaultAsync(c => c.Id == CommentId && c.UserId == UserId, ct);
        if (comment is null) return false;
        db.RecipeComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return true;
    }
}