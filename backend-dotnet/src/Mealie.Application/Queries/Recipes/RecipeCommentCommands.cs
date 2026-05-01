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
        if (recipe is null) return [];
        return await db.RecipeComments
            .Where(c => c.RecipeId == recipe.Id)
            .OrderBy(c => c.CreatedAt)
            .Select(c => CommentMappings.MapToResponse(c))
            .ToListAsync(ct);
    }
}

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

public record GetCommentByIdQuery(Guid CommentId) : IQuery<CommentResponse?>
{
    public async Task<CommentResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var comment = await services.Db.RecipeComments.FirstOrDefaultAsync(c => c.Id == CommentId, ct);
        return comment is null ? null : CommentMappings.MapToResponse(comment);
    }
}

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

public record AddCommentByRecipeIdCommand(Guid RecipeId, Guid UserId, CreateCommentRequest Request) : IQuery<CommentResponse?>
{
    public async Task<CommentResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == RecipeId, ct);
        if (recipe is null) return null;
        var comment = new RecipeComment
        {
            Id = Guid.NewGuid(), Text = Request.Text, RecipeId = RecipeId, UserId = UserId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.RecipeComments.Add(comment);
        await db.SaveChangesAsync(ct);
        return CommentMappings.MapToResponse(comment);
    }
}

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

file static class CommentMappings
{
    public static CommentResponse MapToResponse(RecipeComment c) => new()
    {
        Id = c.Id, Text = c.Text, RecipeId = c.RecipeId, UserId = c.UserId,
        CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt
    };
}
