namespace Mealie.Application.Dtos.Recipes;

public class CommentResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CreateCommentRequest
{
    public string Text { get; set; } = string.Empty;
}

public class UpdateCommentRequest
{
    public string Text { get; set; } = string.Empty;
}
