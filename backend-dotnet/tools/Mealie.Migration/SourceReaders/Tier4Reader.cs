using System.Data.Common;
using Dapper;

namespace Mealie.Migration.SourceReaders;

public class Tier4Reader(DbConnection conn)
{
    public async Task<MigrationData> ReadAllAsync()
    {
        var data = new MigrationData();

        // Planning
        // household_id is an association proxy in Python (not a stored column), so join through users.
        data.MealPlans = (await conn.QueryAsync("""
                                                SELECT gmp.*, u.household_id
                                                FROM group_meal_plans gmp
                                                LEFT JOIN users u ON u.id = gmp.user_id
                                                """)).ToList();
        data.ShoppingLists = (await conn.QueryAsync("SELECT * FROM shopping_lists")).ToList();
        data.ShoppingListItems = (await conn.QueryAsync("SELECT * FROM shopping_list_items")).ToList();
        try
        {
            data.Webhooks = (await conn.QueryAsync("SELECT * FROM webhooks")).ToList();
        }
        catch
        {
        }

        try
        {
            data.EventNotifiers = (await conn.QueryAsync("SELECT * FROM group_event_notifier")).ToList();
        }
        catch
        {
        }

        // Junction tables
        try
        {
            data.RecipeTags = (await conn.QueryAsync("SELECT * FROM recipes_to_tags")).ToList();
        }
        catch
        {
        }

        try
        {
            data.RecipeCategories = (await conn.QueryAsync("SELECT * FROM recipes_to_categories")).ToList();
        }
        catch
        {
        }

        try
        {
            data.RecipeTools = (await conn.QueryAsync("SELECT * FROM recipes_to_tools")).ToList();
        }
        catch
        {
        }

        try
        {
            data.UserToRecipe = (await conn.QueryAsync("SELECT * FROM users_to_favorites")).ToList();
        }
        catch
        {
        }

        try
        {
            data.HouseholdFoods = (await conn.QueryAsync("SELECT * FROM household_ingredient_extras")).ToList();
        }
        catch
        {
        }

        try
        {
            data.CookbookRecipes = (await conn.QueryAsync("SELECT * FROM cookbooks_to_recipes")).ToList();
        }
        catch
        {
        }

        return data;
    }
}
