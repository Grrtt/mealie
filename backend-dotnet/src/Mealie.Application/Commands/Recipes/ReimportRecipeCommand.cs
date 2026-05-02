using Mealie.Application.Common;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Parser;
using Mealie.Infrastructure.Scraper;
using Microsoft.EntityFrameworkCore;
using NutritionDto = Mealie.Application.Dtos.Recipes.NutritionDto;

namespace Mealie.Application.Commands.Recipes;

/// <summary>
///     Replaces a recipe's content (name, times, ingredients, instructions) by re-scraping
///     its original URL. Notes, tags, categories, assets, and other user-added data are preserved.
/// </summary>
public record ReimportRecipeCommand(Guid GroupId, string Slug, ScrapedRecipeDto Scraped)
    : IQuery<RecipeDetailResponse?>
{
    public async Task<RecipeDetailResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == GroupId && r.Slug == Slug)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Unit)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Food)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Assets)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);

        if (recipe is null)
            return null;

        // Update scalar fields from the fresh scrape
        recipe.Name = Scraped.Name ?? recipe.Name;
        recipe.Description = Scraped.Description ?? recipe.Description;
        recipe.RecipeYield = Scraped.RecipeYield ?? recipe.RecipeYield;
        recipe.TotalTime = Scraped.TotalTime ?? recipe.TotalTime;
        recipe.PrepTime = Scraped.PrepTime ?? recipe.PrepTime;
        recipe.CookTime = Scraped.CookTime ?? recipe.CookTime;
        recipe.UpdateAt = DateTime.UtcNow;

        if (Scraped.Nutrition is not null)
        {
            recipe.Nutrition ??= new Nutrition();
            recipe.Nutrition.Calories = Scraped.Nutrition.Calories;
            recipe.Nutrition.FatContent = Scraped.Nutrition.FatContent;
            recipe.Nutrition.ProteinContent = Scraped.Nutrition.ProteinContent;
            recipe.Nutrition.CarbohydrateContent = Scraped.Nutrition.CarbohydrateContent;
        }

        // Replace ingredients — use FullParser so the admin-configured default strategy is respected
        db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
        recipe.RecipeIngredients.Clear();

        var ingredientStrings = Scraped.RecipeIngredient
            .Select(IngredientNormalizer.Normalize)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var parsed = await services.FullParser.ParseBatchAsync(GroupId, ingredientStrings, parserKey: null, ct);

        var foods = await db.Foods.IgnoreQueryFilters()
            .Where(f => f.GroupId == GroupId).Include(f => f.Aliases).ToListAsync(ct);
        var units = await db.Units.IgnoreQueryFilters()
            .Where(u => u.GroupId == GroupId).ToListAsync(ct);

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

            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(), Position = i,
                OriginalText = p.Input ?? ingredientStrings.ElementAtOrDefault(i),
                Note = p.Ingredient.Note,
                Quantity = p.Ingredient.Quantity,
                FoodId = matchedFood?.Id, UnitId = matchedUnit?.Id, RecipeId = recipe.Id
            });
        }

        // Replace instructions
        db.RecipeInstructions.RemoveRange(recipe.RecipeInstructions);
        recipe.RecipeInstructions.Clear();

        for (var i = 0; i < Scraped.RecipeInstructions.Count; i++)
        {
            recipe.RecipeInstructions.Add(new RecipeInstruction
            {
                Id = Guid.NewGuid(), Position = i, Text = Scraped.RecipeInstructions[i], RecipeId = recipe.Id
            });
        }

        await db.SaveChangesAsync(ct);
        return RecipeCommandMappings.MapToDetail(recipe);
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
