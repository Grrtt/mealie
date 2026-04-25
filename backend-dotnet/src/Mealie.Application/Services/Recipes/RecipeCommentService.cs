using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Recipes;

public class RecipeCommentService(ApplicationDbContext db) : IRecipeCommentService
{
    public async Task<IList<CommentResponse>> GetCommentsAsync(string slug, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (recipe is null) return [];

        return await db.RecipeComments
            .Where(c => c.RecipeId == recipe.Id)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponse
            {
                Id = c.Id,
                Text = c.Text,
                RecipeId = c.RecipeId,
                UserId = c.UserId,
                CreatedAt = c.CreatedAt,
                UpdateAt = c.UpdateAt,
            })
            .ToListAsync(ct);
    }

    public async Task<CommentResponse?> AddCommentAsync(string slug, Guid userId, CreateCommentRequest request, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (recipe is null) return null;

        var comment = new RecipeComment
        {
            Id = Guid.NewGuid(),
            Text = request.Text,
            RecipeId = recipe.Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
        };

        db.RecipeComments.Add(comment);
        await db.SaveChangesAsync(ct);

        return new CommentResponse
        {
            Id = comment.Id,
            Text = comment.Text,
            RecipeId = comment.RecipeId,
            UserId = comment.UserId,
            CreatedAt = comment.CreatedAt,
            UpdateAt = comment.UpdateAt,
        };
    }

    public async Task<CommentResponse?> UpdateCommentAsync(Guid commentId, Guid userId, UpdateCommentRequest request, CancellationToken ct = default)
    {
        var comment = await db.RecipeComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId, ct);
        if (comment is null) return null;

        comment.Text = request.Text;
        comment.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new CommentResponse
        {
            Id = comment.Id,
            Text = comment.Text,
            RecipeId = comment.RecipeId,
            UserId = comment.UserId,
            CreatedAt = comment.CreatedAt,
            UpdateAt = comment.UpdateAt,
        };
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default)
    {
        var comment = await db.RecipeComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId, ct);
        if (comment is null) return false;

        db.RecipeComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
