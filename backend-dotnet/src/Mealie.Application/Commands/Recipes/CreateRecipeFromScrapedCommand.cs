using Mealie.Application.Common;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Services.ImageScrape;
using Mealie.Application.Services.Parser;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Recipes;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Parser;
using Mealie.Infrastructure.Scraper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutritionDto = Mealie.Application.Dtos.Recipes.NutritionDto;

namespace Mealie.Application.Commands.Recipes;

public record CreateRecipeFromScrapedCommand(
    ScrapedRecipeDto Scraped,
    Guid HouseholdId,
    Guid GroupId,
    IReadOnlyList<ParsedIngredientDto>? ParsedIngredients = null,
    List<IngredientFood>? CachedFoods = null,
    List<IngredientUnit>? CachedUnits = null,
    Guid? UserId = null)
    : IQuery<RecipeSummaryResponse?>
{
    public async Task<RecipeSummaryResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var logger = services.LoggerFactory.CreateLogger("RecipeCommands");
        var slug = SlugHelper.Generate(Scraped.Name ?? "untitled");
        var existingSlug = await db.Recipes.IgnoreQueryFilters()
            .AnyAsync(r => r.HouseholdId == HouseholdId && r.Slug == slug, ct);
        if (existingSlug)
            return null;

        var uniqueSlug = await RecipeCommandMappings.EnsureUniqueSlugAsync(db, slug, ct);
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(), Name = Scraped.Name ?? "Untitled Recipe", Slug = uniqueSlug,
            Description = Scraped.Description, RecipeYield = Scraped.RecipeYield,
            TotalTime = Scraped.TotalTime, PrepTime = Scraped.PrepTime, CookTime = Scraped.CookTime,
            GroupId = GroupId, HouseholdId = HouseholdId, OrgUrl = Scraped.OrgUrl,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };

        var ingredientStrings = ParsedIngredients is null
            ? Scraped.RecipeIngredient.Select(IngredientNormalizer.Normalize)
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
            : Scraped.RecipeIngredient;

        var parsed = ParsedIngredients
                     ?? (IReadOnlyList<ParsedIngredientDto>)await services.FullParser.ParseBatchAsync(
                         GroupId, ingredientStrings, null, ct);

        // Call organizer after parsing so it receives the same structured ingredient data shown to the user.
        // Non-AI strategies return null silently.
        var organizerSuggestions = await RecipeImportHelpers.SuggestOrganizersAsync(
            services.RecipeOrganizer,
            Scraped.Name ?? "Untitled", Scraped.Description,
            (IList<ParsedIngredientDto>)parsed, Scraped.Categories, Scraped.Keywords, ct);

        var foods = CachedFoods ?? await db.Foods.IgnoreQueryFilters()
            .Where(f => f.GroupId == GroupId).Include(f => f.Aliases).ToListAsync(ct);
        var units = CachedUnits ?? await db.Units.IgnoreQueryFilters()
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
                    string.Equals(f.Name, foodName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.PluralName, foodName, StringComparison.OrdinalIgnoreCase) ||
                    f.Aliases.Any(a => string.Equals(a.Name, foodName, StringComparison.OrdinalIgnoreCase)));
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
                    string.Equals(u.Name, unitName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.PluralName, unitName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.Abbreviation, unitName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.PluralAbbreviation, unitName, StringComparison.OrdinalIgnoreCase));
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

        for (var i = 0; i < Scraped.RecipeInstructions.Count; i++)
        {
            recipe.RecipeInstructions.Add(new RecipeInstruction
                { Id = Guid.NewGuid(), Position = i, Text = Scraped.RecipeInstructions[i], RecipeId = recipe.Id });
        }

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct);

        var tags = await RecipeImportHelpers.ResolveTagsAsync(
            db, GroupId, Scraped.Keywords.Concat(organizerSuggestions?.Tags ?? []), ct);
        foreach (var tag in tags)
            recipe.Tags.Add(tag);

        var categories = await RecipeImportHelpers.ResolveCategoriesAsync(
            db, GroupId, Scraped.Categories.Concat(organizerSuggestions?.Categories ?? []), ct);
        foreach (var cat in categories)
            recipe.Categories.Add(cat);

        if (tags.Count > 0 || categories.Count > 0)
            await db.SaveChangesAsync(ct);

        var hasDirectImage = !string.IsNullOrEmpty(Scraped.Image);
        var hasOrgUrl = !string.IsNullOrEmpty(Scraped.OrgUrl);
        logger.LogInformation(
            "Image queue check for recipe {RecipeId}: hasDirectImage={HasDirectImage}, hasOrgUrl={HasOrgUrl}",
            recipe.Id, hasDirectImage, hasOrgUrl);
        if (hasDirectImage || hasOrgUrl)
        {
            logger.LogInformation("Queuing image scrape job for recipe {RecipeId}", recipe.Id);
            await services.ImageScrapeQueue.Writer.WriteAsync(new ImageScrapeJob(
                    recipe.Id, hasOrgUrl ? Scraped.OrgUrl : null, hasDirectImage ? Scraped.Image : null),
                CancellationToken.None);
        }

        await services.Mediator.Publish(new RecipeCreatedEvent(recipe.Id, HouseholdId, UserId), CancellationToken.None);
        return RecipeCommandMappings.MapToSummary(recipe);
    }
}

file static class RecipeCommandMappings
{
    public static async Task<string> EnsureUniqueSlugAsync(ApplicationDbContext db, string slug, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Slug == candidate, ct))
        {
            candidate = $"{slug}-{counter++}";
        }

        return candidate;
    }

    public static RecipeSummaryResponse MapToSummary(Recipe r)
    {
        return new RecipeSummaryResponse
        {
            Id = r.Id, Name = r.Name, Slug = r.Slug, Description = System.Net.WebUtility.HtmlDecode(r.Description),
            Image = r.Image, OrgUrl = r.OrgUrl, Rating = r.Rating,
            GroupId = r.GroupId, HouseholdId = r.HouseholdId, CreatedAt = r.CreatedAt, UpdateAt = r.UpdateAt,
            Tags = r.Tags.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
            Categories = r.Categories.Select(c => new OrganizerSimpleResponse
                { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList()
        };
    }

    public static RecipeDetailResponse MapToDetail(Recipe r)
    {
        return new RecipeDetailResponse
        {
            Id = r.Id, Name = r.Name, Slug = r.Slug, Description = System.Net.WebUtility.HtmlDecode(r.Description),
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
