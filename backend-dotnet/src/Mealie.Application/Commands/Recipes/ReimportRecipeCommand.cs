using Mealie.Application.Common;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Parser;
using Mealie.Infrastructure.Scraper;
using Microsoft.EntityFrameworkCore;
using NutritionDto = Mealie.Application.Dtos.Recipes.NutritionDto;

namespace Mealie.Application.Commands.Recipes;

/// <summary>
///     Replaces a recipe's content (name, times, ingredients, instructions, tags, categories)
///     by re-scraping its original URL. Notes, assets, and other user-added data are preserved.
/// </summary>
public record ReimportRecipeCommand(Guid GroupId, string Slug, ScrapedRecipeDto Scraped)
    : IQuery<RecipeDetailResponse?>
{
    public async Task<RecipeDetailResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;

        // Load current values AsNoTracking — we never put the Recipe in the change tracker.
        // All mutations go through ExecuteDeleteAsync / ExecuteUpdateAsync (raw SQL), so
        // SaveChangesAsync only ever sees INSERTs (new ingredients/instructions/foods/units),
        // which avoids the SQLite AffectedCountModificationCommandBatch "expected 1, got 0" bug
        // that occurs when a tracked Recipe UPDATE follows an ExecuteDeleteAsync on the same context.
        var current = await db.Recipes.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.GroupId == GroupId && r.Slug == Slug)
            .FirstOrDefaultAsync(ct);

        if (current is null)
            return null;

        // Step 1 — Delete old ingredients, instructions, and tag/category associations via raw SQL.
        await db.RecipeIngredients.Where(i => i.RecipeId == current.Id).ExecuteDeleteAsync(ct);
        await db.RecipeInstructions.Where(i => i.RecipeId == current.Id).ExecuteDeleteAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM recipes_to_tags WHERE recipe_id = {current.Id}", ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM recipes_to_categories WHERE recipe_id = {current.Id}", ct);

        // Step 2 — Update recipe scalar fields via raw SQL.
        await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.Id == current.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Name, Scraped.Name ?? current.Name)
                .SetProperty(r => r.Description, Scraped.Description)
                .SetProperty(r => r.RecipeYield, Scraped.RecipeYield)
                .SetProperty(r => r.TotalTime, Scraped.TotalTime)
                .SetProperty(r => r.PrepTime, Scraped.PrepTime)
                .SetProperty(r => r.CookTime, Scraped.CookTime)
                .SetProperty(r => r.UpdateAt, DateTime.UtcNow), ct);

        if (Scraped.Nutrition is not null)
        {
            await db.Recipes.IgnoreQueryFilters()
                .Where(r => r.Id == current.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Nutrition!.Calories, Scraped.Nutrition.Calories)
                    .SetProperty(r => r.Nutrition!.FatContent, Scraped.Nutrition.FatContent)
                    .SetProperty(r => r.Nutrition!.ProteinContent, Scraped.Nutrition.ProteinContent)
                    .SetProperty(r => r.Nutrition!.CarbohydrateContent, Scraped.Nutrition.CarbohydrateContent), ct);
        }

        // Step 3 — Parse and match ingredients.
        var ingredientStrings = Scraped.RecipeIngredient
            .Select(IngredientNormalizer.Normalize)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var parsed = await services.FullParser.ParseBatchAsync(GroupId, ingredientStrings, parserKey: null, ct);

        var foods = await db.Foods.IgnoreQueryFilters()
            .Where(f => f.GroupId == GroupId).Include(f => f.Aliases).ToListAsync(ct);
        var units = await db.Units.IgnoreQueryFilters()
            .Where(u => u.GroupId == GroupId).ToListAsync(ct);

        var newIngredients = new List<RecipeIngredient>();
        for (var i = 0; i < parsed.Count; i++)
        {
            var p = parsed[i];
            var foodName = p.Ingredient.Food?.Name;
            var unitName = p.Ingredient.Unit?.Name;

            IngredientFood? matchedFood = null;
            if (!string.IsNullOrWhiteSpace(foodName))
            {
                matchedFood = foods.FirstOrDefault(f =>
                    string.Equals(f.Name, foodName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(f.PluralName, foodName, StringComparison.OrdinalIgnoreCase)
                    || f.Aliases.Any(a => string.Equals(a.Name, foodName, StringComparison.OrdinalIgnoreCase)));

                if (matchedFood is null)
                {
                    matchedFood = new IngredientFood
                    {
                        Id = Guid.NewGuid(), Name = foodName, GroupId = GroupId,
                        CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                    };
                    db.Foods.Add(matchedFood);
                    foods.Add(matchedFood);
                }
            }

            IngredientUnit? matchedUnit = null;
            if (!string.IsNullOrWhiteSpace(unitName))
            {
                matchedUnit = units.FirstOrDefault(u =>
                    string.Equals(u.Name, unitName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(u.PluralName, unitName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(u.Abbreviation, unitName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(u.PluralAbbreviation, unitName, StringComparison.OrdinalIgnoreCase));

                if (matchedUnit is null)
                {
                    matchedUnit = new IngredientUnit
                    {
                        Id = Guid.NewGuid(), Name = unitName, GroupId = GroupId,
                        CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                    };
                    db.Units.Add(matchedUnit);
                    units.Add(matchedUnit);
                }
            }

            newIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(), Position = i,
                OriginalText = p.Input ?? ingredientStrings.ElementAtOrDefault(i),
                Note = p.Ingredient.Note,
                Quantity = p.Ingredient.Quantity,
                FoodId = matchedFood?.Id, UnitId = matchedUnit?.Id, RecipeId = current.Id
            });
        }

        var newInstructions = Scraped.RecipeInstructions.Select((text, i) => new RecipeInstruction
        {
            Id = Guid.NewGuid(), Position = i, Text = text, RecipeId = current.Id
        }).ToList();

        // Step 4 — Resolve tags and categories (create new organizers if they don't exist yet).
        var resolvedTags = new List<Tag>();
        foreach (var keyword in Scraped.Keywords)
        {
            var tagName = keyword.Trim();
            if (string.IsNullOrEmpty(tagName))
                continue;

            var tagSlug = SlugHelper.Generate(tagName);
            var tag = await db.Tags.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == tagSlug && t.GroupId == GroupId, ct);
            if (tag is null)
            {
                tag = new Tag
                {
                    Id = Guid.NewGuid(), Name = tagName, Slug = tagSlug, GroupId = GroupId,
                    CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                };
                db.Tags.Add(tag);
            }

            resolvedTags.Add(tag);
        }

        var resolvedCategories = new List<Category>();
        foreach (var catName in Scraped.Categories)
        {
            var name = catName.Trim();
            if (string.IsNullOrEmpty(name))
                continue;

            var catSlug = SlugHelper.Generate(name);
            var cat = await db.Categories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == GroupId, ct);
            if (cat is null)
            {
                cat = new Category
                {
                    Id = Guid.NewGuid(), Name = name, Slug = catSlug, GroupId = GroupId,
                    CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                };
                db.Categories.Add(cat);
            }

            resolvedCategories.Add(cat);
        }

        // Step 5 — Persist only INSERTs through the change tracker (no UPDATE/DELETE paths).
        // Includes any new tags/categories, foods, units, ingredients, and instructions.
        db.RecipeIngredients.AddRange(newIngredients);
        db.RecipeInstructions.AddRange(newInstructions);
        await db.SaveChangesAsync(ct);

        // Step 6 — Insert tag and category join-table rows via raw SQL (safe after SaveChangesAsync
        // has persisted any newly created tags/categories above).
        foreach (var tag in resolvedTags)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO recipes_to_tags (recipe_id, tag_id) VALUES ({current.Id}, {tag.Id})", ct);
        }

        foreach (var cat in resolvedCategories)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO recipes_to_categories (category_id, recipe_id) VALUES ({cat.Id}, {current.Id})", ct);
        }

        // Step 7 — Reload the full recipe (with all relationships) for the response.
        var updated = await db.Recipes.IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Id == current.Id)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Unit)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Food)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Assets)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);

        return updated is null ? null : RecipeCommandMappings.MapToDetail(updated);
    }
}

file static class RecipeCommandMappings
{
    public static RecipeDetailResponse MapToDetail(Recipe r)
    {
        return new RecipeDetailResponse
        {
            Id = r.Id, Name = r.Name, Slug = r.Slug, Description = r.Description,
            RecipeYield = r.RecipeYield, TotalTime = r.TotalTime, PrepTime = r.PrepTime,
            CookTime = r.CookTime, PerformTime = r.PerformTime, Rating = r.Rating,
            DisableAmount = r.DisableAmount, Image = r.Image, OrgUrl = r.OrgUrl,
            GroupId = r.GroupId, HouseholdId = r.HouseholdId, CreatedAt = r.CreatedAt, UpdateAt = r.UpdateAt,
            LastMade = r.LastMade,
            Nutrition = r.Nutrition is null
                ? null
                : new NutritionDto
                {
                    Calories = r.Nutrition.Calories, FatContent = r.Nutrition.FatContent,
                    ProteinContent = r.Nutrition.ProteinContent, CarbohydrateContent = r.Nutrition.CarbohydrateContent,
                    FiberContent = r.Nutrition.FiberContent, SodiumContent = r.Nutrition.SodiumContent,
                    SugarContent = r.Nutrition.SugarContent
                },
            Settings = new RecipeSettingsDto
            {
                Public = r.Settings?.Public ?? false, ShowNutrition = r.Settings?.ShowNutrition ?? false,
                ShowAssets = r.Settings?.ShowAssets ?? false, LandscapeView = r.Settings?.LandscapeView ?? false,
                DisableComments = r.Settings?.DisableComments ?? false,
                DisableAmount = r.Settings?.DisableAmount ?? false,
                Locked = r.Settings?.Locked ?? false
            },
            RecipeIngredients = r.RecipeIngredients.Select(i => new RecipeIngredientDto
            {
                Id = i.Id, Position = i.Position, Title = i.Title, Note = i.Note,
                Quantity = i.Quantity, OriginalText = i.OriginalText, IsFood = i.IsFood,
                DisableAmount = i.DisableAmount,
                Unit = i.Unit is null
                    ? null
                    : new RecipeIngredientUnitDto
                        { Id = i.Unit.Id, Name = i.Unit.Name, Abbreviation = i.Unit.Abbreviation },
                Food = i.Food is null ? null : new RecipeIngredientFoodDto { Id = i.Food.Id, Name = i.Food.Name }
            }).ToList(),
            RecipeInstructions = r.RecipeInstructions.Select(i => new RecipeInstructionDto
                { Id = i.Id, Position = i.Position, Text = i.Text, Title = i.Title, Summary = i.Summary }).ToList(),
            Notes = r.Notes.Select(n => new RecipeNoteDto { Id = n.Id, Title = n.Title, Text = n.Text }).ToList(),
            Assets = r.Assets.Select(a => new RecipeAssetDto
                { Id = a.Id, Name = a.Name, Icon = a.Icon, FileName = $"{a.Name}.{a.Extension}" }).ToList(),
            Tags = r.Tags.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
            Categories = r.Categories.Select(c => new OrganizerSimpleResponse
                { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList(),
            Tools = r.Tools.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug })
                .ToList()
        };
    }
}
