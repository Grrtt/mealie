using Mealie.Application.Dtos.Recipes;

namespace Mealie.Application.Services.Recipes;

public interface IRecipeCommentService
{
    Task<IList<CommentResponse>> GetCommentsAsync(string slug, CancellationToken ct = default);

    Task<CommentResponse?> AddCommentAsync(string slug, Guid userId, CreateCommentRequest request,
        CancellationToken ct = default);

    Task<CommentResponse?> UpdateCommentAsync(Guid commentId, Guid userId, UpdateCommentRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default);
    Task<IList<CommentResponse>> GetAllCommentsAsync(Guid groupId, CancellationToken ct = default);
    Task<CommentResponse?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default);

    Task<CommentResponse?> AddCommentByRecipeIdAsync(Guid recipeId, Guid userId, CreateCommentRequest request,
        CancellationToken ct = default);
}
