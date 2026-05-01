namespace Mealie.Migration.SourceReaders;

public class MigrationData
{
    // Groups
    public List<dynamic> Groups { get; set; } = [];
    public List<dynamic> Households { get; set; } = [];
    public List<dynamic> Users { get; set; } = [];
    public List<dynamic> ApiKeys { get; set; } = [];
    public List<dynamic> GroupPreferences { get; set; } = [];
    public List<dynamic> HouseholdPreferences { get; set; } = [];
    public List<dynamic> MultiPurposeLabels { get; set; } = [];

    // Organizers
    public List<dynamic> Units { get; set; } = [];
    public List<dynamic> Foods { get; set; } = [];
    public List<dynamic> FoodAliases { get; set; } = [];
    public List<dynamic> Tags { get; set; } = [];
    public List<dynamic> Categories { get; set; } = [];
    public List<dynamic> Tools { get; set; } = [];
    public List<dynamic> Cookbooks { get; set; } = [];

    // Recipes
    public List<dynamic> Recipes { get; set; } = [];
    public List<dynamic> Ingredients { get; set; } = [];
    public List<dynamic> Instructions { get; set; } = [];
    public List<dynamic> Notes { get; set; } = [];
    public List<dynamic> Assets { get; set; } = [];
    public List<dynamic> Comments { get; set; } = [];
    public List<dynamic> TimelineEvents { get; set; } = [];
    public List<dynamic> ShareTokens { get; set; } = [];

    // Junction / Planning
    public List<dynamic> MealPlans { get; set; } = [];
    public List<dynamic> ShoppingLists { get; set; } = [];
    public List<dynamic> ShoppingListItems { get; set; } = [];
    public List<dynamic> Webhooks { get; set; } = [];
    public List<dynamic> EventNotifiers { get; set; } = [];
    public List<dynamic> LongLiveTokens { get; set; } = [];

    // Junction tables
    public List<dynamic> RecipeTags { get; set; } = [];
    public List<dynamic> RecipeCategories { get; set; } = [];
    public List<dynamic> RecipeTools { get; set; } = [];
    public List<dynamic> UserToRecipe { get; set; } = [];
    public List<dynamic> HouseholdFoods { get; set; } = [];
    public List<dynamic> CookbookRecipes { get; set; } = [];

    public int TotalCount => Groups.Count + Households.Count + Users.Count +
                             Units.Count + Foods.Count + Tags.Count + Categories.Count + Tools.Count + Cookbooks.Count +
                             Recipes.Count + Ingredients.Count + Instructions.Count +
                             MealPlans.Count + ShoppingLists.Count + ShoppingListItems.Count;
}
