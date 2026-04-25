using Dapper;
using System.Data.Common;

namespace Mealie.Migration.SourceReaders;

public class Tier4Reader(DbConnection conn)
{
    public async Task<MigrationData> ReadAllAsync()
    {
        var data = new MigrationData();

        // Planning
        data.MealPlans = (await conn.QueryAsync("SELECT * FROM meal_plans")).ToList<dynamic>();
        data.ShoppingLists = (await conn.QueryAsync("SELECT * FROM shopping_lists")).ToList<dynamic>();
        data.ShoppingListItems = (await conn.QueryAsync("SELECT * FROM shopping_list_items")).ToList<dynamic>();
        try { data.Webhooks = (await conn.QueryAsync("SELECT * FROM webhooks")).ToList<dynamic>(); } catch { }
        try { data.EventNotifiers = (await conn.QueryAsync("SELECT * FROM group_event_notifier")).ToList<dynamic>(); } catch { }

        // Junction tables
        try { data.RecipeTags = (await conn.QueryAsync("SELECT * FROM recipes_to_tags")).ToList<dynamic>(); } catch { }
        try { data.RecipeCategories = (await conn.QueryAsync("SELECT * FROM recipes_to_categories")).ToList<dynamic>(); } catch { }
        try { data.RecipeTools = (await conn.QueryAsync("SELECT * FROM recipes_to_tools")).ToList<dynamic>(); } catch { }
        try { data.UserToRecipe = (await conn.QueryAsync("SELECT * FROM users_to_favorites")).ToList<dynamic>(); } catch { }
        try { data.HouseholdFoods = (await conn.QueryAsync("SELECT * FROM household_ingredient_extras")).ToList<dynamic>(); } catch { }
        try { data.CookbookRecipes = (await conn.QueryAsync("SELECT * FROM cookbooks_to_recipes")).ToList<dynamic>(); } catch { }

        return data;
    }
}
