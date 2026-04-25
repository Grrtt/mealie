using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Recipes;

public class RecipeComment
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public User User { get; set; } = null!;
}
