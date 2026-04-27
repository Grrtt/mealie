namespace Mealie.Application.Dtos.Ingredients;

public class UnresolvedIngredientResponse
{
    public string RawText { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class IngredientAliasResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid FoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
}

public class CreateIngredientAliasRequest
{
    public string RawText { get; set; } = string.Empty;
    public Guid FoodId { get; set; }
    public bool BackfillRecipes { get; set; } = true;
}
