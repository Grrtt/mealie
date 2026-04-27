namespace Mealie.Application.Dtos.MealPlans;

public class MealPlanRecipeTagSummary
{
    public string Id { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class MealPlanRecipeSummary
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Image { get; set; }
    public string? Description { get; set; }
    public IList<MealPlanRecipeTagSummary> Tags { get; set; } = [];
    public IList<MealPlanRecipeTagSummary> RecipeCategory { get; set; } = [];
}

public class MealPlanResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public Guid? RecipeId { get; set; }
    public MealPlanRecipeSummary? Recipe { get; set; }
    public Guid GroupId { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }
}

public class CreateMealPlanRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string EntryType { get; set; } = "breakfast";
    public DateOnly Date { get; set; }
    public Guid? RecipeId { get; set; }
}

public class UpdateMealPlanRequest
{
    public string? Title { get; set; }
    public string? Text { get; set; }
    public string? EntryType { get; set; }
    public DateOnly? Date { get; set; }
    public Guid? RecipeId { get; set; }
}

public class CreateRandomMealPlanRequest
{
    public DateOnly Date { get; set; }
    public string EntryType { get; set; } = "breakfast";
}
