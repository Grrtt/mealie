using System.Data.Common;
using Dapper;
using Mealie.Migration.SourceReaders;
using Serilog;

namespace Mealie.Migration.TargetWriters;

public class TargetWriter(DbConnection conn, ILogger log)
{
    public async Task WriteAsync(MigrationData data, MigrationReport report)
    {
        await WriteEntitiesAsync("groups", data.Groups, report, async row =>
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO groups (id, name, slug, created_at, update_at)
                                    VALUES (@id, @name, @slug, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, new
            {
                id = (string)row.id, name = (string)row.name, slug = (string?)row.slug, row.created_at,
                row.update_at
            });
        });

        await WriteEntitiesAsync("households", data.Households, report, async row =>
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO households (id, name, slug, group_id, created_at, update_at)
                                    VALUES (@id, @name, @slug, @group_id, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, new
            {
                id = (string)row.id, name = (string)row.name, slug = (string?)row.slug, group_id = (string)row.group_id,
                row.created_at,
                row.update_at
            });
        });

        await WriteEntitiesAsync("users", data.Users, report, async row =>
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO users (id, full_name, username, email, password, admin, advanced, group_id, household_id, auth_method, can_manage, can_invite, can_organize, can_manage_household, created_at, update_at)
                                    VALUES (@id, @full_name, @username, @email, @password, @admin, @advanced, @group_id, @household_id, @auth_method, @can_manage, @can_invite, @can_organize, @can_manage_household, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, new
            {
                id = (string)row.id,
                full_name = (string?)row.full_name,
                username = (string?)row.username,
                email = (string?)row.email,
                password = (string?)row.password,
                admin = (bool?)row.admin ?? false,
                advanced = (bool?)row.advanced ?? false,
                group_id = (string)row.group_id,
                household_id = (string?)row.household_id,
                auth_method = (string?)row.auth_method ?? "Mealie",
                can_manage = (bool?)row.can_manage ?? false,
                can_invite = (bool?)row.can_invite ?? false,
                can_organize = (bool?)row.can_organize ?? false,
                can_manage_household = (bool?)row.can_manage_household ?? false,
                row.created_at,
                update_at = row.update_at ?? row.created_at
            });
        });

        await WriteEntitiesAsync("recipes", data.Recipes, report, async row =>
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO recipes (id, name, slug, description, image, total_time, prep_time, perform_time, rating, group_id, household_id, user_id, created_at, update_at)
                                    VALUES (@id, @name, @slug, @description, @image, @total_time, @prep_time, @perform_time, @rating, @group_id, @household_id, @user_id, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, new
            {
                id = (string)row.id,
                name = (string)row.name,
                slug = (string)row.slug,
                description = (string?)row.description,
                image = (string?)row.image,
                total_time = (string?)row.total_time,
                prep_time = (string?)row.prep_time,
                perform_time = (string?)row.perform_time,
                rating = (int?)row.rating,
                group_id = (string)row.group_id,
                household_id = (string?)row.household_id,
                user_id = (string?)row.user_id,
                row.created_at,
                update_at = row.update_at ?? row.created_at
            });
        });

        await WriteEntitiesAsync("ingredients", data.Ingredients, report, async row =>
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO recipes_ingredients (id, recipe_id, title, note, unit_id, food_id, disable_amount, quantity, original_text, reference_id, created_at, update_at)
                                    VALUES (@id, @recipe_id, @title, @note, @unit_id, @food_id, @disable_amount, @quantity, @original_text, @reference_id, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, new
            {
                id = (string)row.id,
                recipe_id = (string)row.recipe_id,
                title = (string?)row.title,
                note = (string?)row.note,
                unit_id = (string?)row.unit_id,
                food_id = (string?)row.food_id,
                disable_amount = (bool?)row.disable_amount ?? false,
                quantity = (double?)row.quantity,
                original_text = (string?)row.original_text,
                reference_id = (string?)row.reference_id,
                row.created_at,
                update_at = row.update_at ?? row.created_at
            });
        });

        await WriteEntitiesAsync("instructions", data.Instructions, report, async row =>
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO recipe_instructions (id, recipe_id, position, title, text, ingredient_references, created_at, update_at)
                                    VALUES (@id, @recipe_id, @position, @title, @text, @ingredient_references, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, new
            {
                id = (string)row.id,
                recipe_id = (string)row.recipe_id,
                position = (int?)row.position ?? 0,
                title = (string?)row.title,
                text = (string?)row.text,
                ingredient_references = (string?)row.ingredient_references,
                row.created_at,
                update_at = row.update_at ?? row.created_at
            });
        });

        await WriteEntitiesAsync("meal_plans", data.MealPlans, report, async row =>
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO group_meal_plans (id, date, entry_type, title, text, recipe_id, group_id, household_id, user_id, created_at, update_at)
                                    VALUES (@id, @date, @entry_type, @title, @text, @recipe_id, @group_id, @household_id, @user_id, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, new
            {
                id = (string)row.id,
                row.date,
                entry_type = (string?)row.entry_type,
                title = (string?)row.title,
                text = (string?)row.text,
                recipe_id = (string?)row.recipe_id,
                group_id = (string)row.group_id,
                household_id = (string?)row.household_id,
                user_id = (string?)row.user_id,
                row.created_at,
                update_at = row.update_at ?? row.created_at
            });
        });

        await WriteEntitiesAsync("shopping_lists", data.ShoppingLists, report, async row =>
        {
            await conn.ExecuteAsync("""
                                    INSERT INTO shopping_lists (id, name, group_id, household_id, created_at, update_at)
                                    VALUES (@id, @name, @group_id, @household_id, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, new
            {
                id = (string)row.id,
                name = (string?)row.name,
                group_id = (string)row.group_id,
                household_id = (string?)row.household_id,
                row.created_at,
                update_at = row.update_at ?? row.created_at
            });
        });

        await WriteEntitiesAsync("shopping_list_items", data.ShoppingListItems, report, async row =>
        {
            // "checked" is a C# keyword, so we read it via dictionary access
            var rowDict = (IDictionary<string, object>)row;
            var isChecked = rowDict.TryGetValue("checked", out var chk) && chk is bool b && b;
            var isFoodVal = rowDict.TryGetValue("is_food", out var isf) && isf is bool f && f;

            var dp = new DynamicParameters();
            dp.Add("id", (string)row.id);
            dp.Add("shopping_list_id", (string)row.shopping_list_id);
            dp.Add("checked", isChecked);
            dp.Add("position", (int?)row.position ?? 0);
            dp.Add("is_food", isFoodVal);
            dp.Add("note", (string?)row.note);
            dp.Add("quantity", (double?)row.quantity);
            dp.Add("food_id", (string?)row.food_id);
            dp.Add("unit_id", (string?)row.unit_id);
            dp.Add("label_id", (string?)row.label_id);
            dp.Add("recipe_id", (string?)row.recipe_id);
            dp.Add("created_at", row.created_at);
            dp.Add("update_at", row.update_at ?? row.created_at);

            await conn.ExecuteAsync("""
                                    INSERT INTO shopping_list_items (id, shopping_list_id, checked, position, is_food, note, quantity, food_id, unit_id, label_id, recipe_id, created_at, update_at)
                                    VALUES (@id, @shopping_list_id, @checked, @position, @is_food, @note, @quantity, @food_id, @unit_id, @label_id, @recipe_id, @created_at, @update_at)
                                    ON CONFLICT (id) DO NOTHING
                                    """, dp);
        });
    }

    private async Task WriteEntitiesAsync(string entityName, List<dynamic> rows, MigrationReport report,
        Func<dynamic, Task> insertFn)
    {
        int migrated = 0, skipped = 0, errors = 0;
        foreach (var row in rows)
        {
            try
            {
                await insertFn(row);
                migrated++;
            }
            catch (Exception ex)
            {
                errors++;
                log.Warning("Skipped {Entity} id={Id}: {Error}", entityName, TryGetId(row), ex.Message);
            }
        }

        report.Add(entityName, migrated, skipped, errors);
    }

    private static string TryGetId(dynamic row)
    {
        try
        {
            return (string)row.id;
        }
        catch
        {
            return "unknown";
        }
    }
}
