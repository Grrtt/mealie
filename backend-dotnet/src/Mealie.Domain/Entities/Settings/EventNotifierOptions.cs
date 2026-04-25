namespace Mealie.Domain.Entities.Settings;

public class EventNotifierOptions
{
    public bool TestMessage { get; set; }
    public bool RecipeCreated { get; set; }
    public bool RecipeUpdated { get; set; }
    public bool RecipeDeleted { get; set; }
    public bool UserSignup { get; set; }
    public bool MealplanEntryCreated { get; set; }
    public bool ShoppingListCreated { get; set; }
    public bool ShoppingListUpdated { get; set; }
    public bool ShoppingListDeleted { get; set; }
}
