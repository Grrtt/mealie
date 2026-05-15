using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

internal static class RecipeUpdatePersistence
{
    public static async Task<Recipe?> UpdateAsync(
        ApplicationDbContext db, Guid groupId, string slug, UpdateRecipeRequest request, CancellationToken ct = default)
    {
        // Keep the recipe itself out of the change tracker so collection replacement work
        // does not mix tracked UPDATEs with dependent DELETEs/INSERTs in one SaveChanges call.
        var recipe = await db.Recipes.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.GroupId == groupId && r.Slug == slug)
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
        {
            return null;
        }

        ApplyScalarUpdates(recipe, request);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        if (request.RecipeIngredients is not null)
        {
            await db.RecipeIngredients.Where(i => i.RecipeId == recipe.Id).ExecuteDeleteAsync(ct);
        }

        if (request.RecipeInstructions is not null)
        {
            await db.RecipeInstructions.Where(i => i.RecipeId == recipe.Id).ExecuteDeleteAsync(ct);
        }

        if (request.Notes is not null)
        {
            await db.RecipeNotes.Where(n => n.RecipeId == recipe.Id).ExecuteDeleteAsync(ct);
        }

        if (request.Tags is not null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM recipes_to_tags WHERE recipe_id = {recipe.Id}", ct);
        }

        if (request.Categories is not null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM recipes_to_categories WHERE recipe_id = {recipe.Id}", ct);
        }

        if (request.Tools is not null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM recipes_to_tools WHERE recipe_id = {recipe.Id}", ct);
        }

        await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Id == recipe.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Name, recipe.Name)
                .SetProperty(r => r.Description, recipe.Description)
                .SetProperty(r => r.RecipeYield, recipe.RecipeYield)
                .SetProperty(r => r.TotalTime, recipe.TotalTime)
                .SetProperty(r => r.PrepTime, recipe.PrepTime)
                .SetProperty(r => r.CookTime, recipe.CookTime)
                .SetProperty(r => r.PerformTime, recipe.PerformTime)
                .SetProperty(r => r.Rating, recipe.Rating)
                .SetProperty(r => r.DisableAmount, recipe.DisableAmount)
                .SetProperty(r => r.OrgUrl, recipe.OrgUrl)
                .SetProperty(r => r.LastMade, recipe.LastMade)
                .SetProperty(r => r.UpdateAt, recipe.UpdateAt), ct);

        if (request.Nutrition is not null && recipe.Nutrition is not null)
        {
            await db.Recipes.IgnoreQueryFilters()
                .Where(r => r.Id == recipe.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Nutrition!.Calories, recipe.Nutrition.Calories)
                    .SetProperty(r => r.Nutrition!.FatContent, recipe.Nutrition.FatContent)
                    .SetProperty(r => r.Nutrition!.ProteinContent, recipe.Nutrition.ProteinContent)
                    .SetProperty(r => r.Nutrition!.CarbohydrateContent, recipe.Nutrition.CarbohydrateContent)
                    .SetProperty(r => r.Nutrition!.FiberContent, recipe.Nutrition.FiberContent)
                    .SetProperty(r => r.Nutrition!.SodiumContent, recipe.Nutrition.SodiumContent)
                    .SetProperty(r => r.Nutrition!.SugarContent, recipe.Nutrition.SugarContent), ct);
        }

        if (request.Settings is not null && recipe.Settings is not null)
        {
            await db.Recipes.IgnoreQueryFilters()
                .Where(r => r.Id == recipe.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Settings!.Public, recipe.Settings.Public)
                    .SetProperty(r => r.Settings!.ShowNutrition, recipe.Settings.ShowNutrition)
                    .SetProperty(r => r.Settings!.ShowAssets, recipe.Settings.ShowAssets)
                    .SetProperty(r => r.Settings!.LandscapeView, recipe.Settings.LandscapeView)
                    .SetProperty(r => r.Settings!.DisableComments, recipe.Settings.DisableComments)
                    .SetProperty(r => r.Settings!.DisableAmount, recipe.Settings.DisableAmount)
                    .SetProperty(r => r.Settings!.Locked, recipe.Settings.Locked), ct);
        }

        if (request.RecipeIngredients is not null)
        {
            db.RecipeIngredients.AddRange(request.RecipeIngredients.Select(ing => new RecipeIngredient
            {
                Id = ing.Id ?? Guid.NewGuid(),
                Position = ing.Position,
                Title = ing.Title,
                Note = ing.Note,
                Quantity = ing.Quantity,
                UnitId = ing.UnitId,
                FoodId = ing.FoodId,
                OriginalText = ing.OriginalText,
                IsFood = ing.IsFood,
                DisableAmount = ing.DisableAmount,
                RecipeId = recipe.Id
            }));
        }

        if (request.RecipeInstructions is not null)
        {
            db.RecipeInstructions.AddRange(request.RecipeInstructions.Select(inst => new RecipeInstruction
            {
                Id = inst.Id ?? Guid.NewGuid(),
                Position = inst.Position,
                Text = inst.Text,
                Title = inst.Title,
                Summary = inst.Summary,
                RecipeId = recipe.Id
            }));
        }

        if (request.Notes is not null)
        {
            db.RecipeNotes.AddRange(request.Notes.Select(note => new RecipeNote
            {
                Id = note.Id ?? Guid.NewGuid(),
                Title = note.Title,
                Text = note.Text,
                RecipeId = recipe.Id
            }));
        }

        await db.SaveChangesAsync(ct);

        if (request.Tags is not null)
        {
            var tagIds = await db.Tags.IgnoreQueryFilters()
                .Where(t => t.GroupId == groupId && request.Tags.Contains(t.Slug))
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var tagId in tagIds)
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO recipes_to_tags (recipe_id, tag_id) VALUES ({recipe.Id}, {tagId})", ct);
            }
        }

        if (request.Categories is not null)
        {
            var categoryIds = await db.Categories.IgnoreQueryFilters()
                .Where(c => c.GroupId == groupId && request.Categories.Contains(c.Slug))
                .Select(c => c.Id)
                .ToListAsync(ct);

            foreach (var categoryId in categoryIds)
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO recipes_to_categories (category_id, recipe_id) VALUES ({categoryId}, {recipe.Id})", ct);
            }
        }

        if (request.Tools is not null)
        {
            var toolIds = await db.Tools.IgnoreQueryFilters()
                .Where(t => t.GroupId == groupId && request.Tools.Contains(t.Slug))
                .Select(t => t.Id)
                .ToListAsync(ct);

            foreach (var toolId in toolIds)
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO recipes_to_tools (recipe_id, tool_id) VALUES ({recipe.Id}, {toolId})", ct);
            }
        }

        await tx.CommitAsync(ct);

        return await db.Recipes.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Id == recipe.Id)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Unit)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Food)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Assets)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);
    }

    private static void ApplyScalarUpdates(Recipe recipe, UpdateRecipeRequest request)
    {
        if (request.Name is not null)
        {
            recipe.Name = request.Name;
        }

        if (request.Description is not null)
        {
            recipe.Description = request.Description;
        }

        if (request.RecipeYield is not null)
        {
            recipe.RecipeYield = request.RecipeYield;
        }

        if (request.TotalTime is not null)
        {
            recipe.TotalTime = request.TotalTime;
        }

        if (request.PrepTime is not null)
        {
            recipe.PrepTime = request.PrepTime;
        }

        if (request.CookTime is not null)
        {
            recipe.CookTime = request.CookTime;
        }

        if (request.PerformTime is not null)
        {
            recipe.PerformTime = request.PerformTime;
        }

        if (request.Rating.HasValue)
        {
            recipe.Rating = request.Rating;
        }

        if (request.DisableAmount.HasValue)
        {
            recipe.DisableAmount = request.DisableAmount.Value;
        }

        if (request.OrgUrl is not null)
        {
            recipe.OrgUrl = request.OrgUrl;
        }

        if (request.LastMade.HasValue)
        {
            recipe.LastMade = request.LastMade;
        }

        if (request.Nutrition is not null)
        {
            recipe.Nutrition ??= new Nutrition();
            recipe.Nutrition.Calories = request.Nutrition.Calories;
            recipe.Nutrition.FatContent = request.Nutrition.FatContent;
            recipe.Nutrition.ProteinContent = request.Nutrition.ProteinContent;
            recipe.Nutrition.CarbohydrateContent = request.Nutrition.CarbohydrateContent;
            recipe.Nutrition.FiberContent = request.Nutrition.FiberContent;
            recipe.Nutrition.SodiumContent = request.Nutrition.SodiumContent;
            recipe.Nutrition.SugarContent = request.Nutrition.SugarContent;
        }

        if (request.Settings is not null)
        {
            recipe.Settings ??= new RecipeSettings();
            recipe.Settings.Public = request.Settings.Public;
            recipe.Settings.ShowNutrition = request.Settings.ShowNutrition;
            recipe.Settings.ShowAssets = request.Settings.ShowAssets;
            recipe.Settings.LandscapeView = request.Settings.LandscapeView;
            recipe.Settings.DisableComments = request.Settings.DisableComments;
            recipe.Settings.DisableAmount = request.Settings.DisableAmount;
            recipe.Settings.Locked = request.Settings.Locked;
        }

        recipe.UpdateAt = DateTime.UtcNow;
    }
}
