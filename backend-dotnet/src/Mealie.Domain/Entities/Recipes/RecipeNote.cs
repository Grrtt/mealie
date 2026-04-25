namespace Mealie.Domain.Entities.Recipes;

public class RecipeNote
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Guid RecipeId { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
