namespace Mealie.Domain.Entities.Recipes;

public class RecipeAsset
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
