using Mealie.Domain.Entities.Ingredients;

namespace Mealie.Domain.Entities.Recipes;

public class RecipeInstruction
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public Guid RecipeId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public ICollection<IngredientFood> IngredientReferences { get; set; } = [];
}
