using Mealie.Domain.Entities.Core;

namespace Mealie.Domain.Entities.Recipes;

public class RecipeShareToken
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid GroupId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public Group Group { get; set; } = null!;
}
