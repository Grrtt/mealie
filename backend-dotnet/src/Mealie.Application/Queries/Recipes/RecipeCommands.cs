using Mealie.Application.Common;
using Mealie.Application.Contracts.Search;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.ImageScrape;
using Mealie.Application.Services.IngredientParser;
using Mealie.Application.Services.Recipes;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Recipes;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Parser;
using Mealie.Infrastructure.Scraper;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutritionDto = Mealie.Application.Dtos.Recipes.NutritionDto;

namespace Mealie.Application.Queries.Recipes;

public record GetAllRecipesQuery : IQuery<IList<RecipeSummaryResponse>>
{
    public async Task<IList<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await services.Db.Recipes
            .Include(r => r.Tags).Include(r => r.Categories)
            .Select(r => RecipeCommandMappings.MapToSummary(r))
            .ToListAsync(ct);
    }
}

public record GetPaginatedRecipesQuery(Guid HouseholdId, PaginationParams Pagination, RecipeFilter? Filter = null)
    : IQuery<PaginatedResponse<RecipeSummaryResponse>>
{
    public async Task<PaginatedResponse<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var searchIndex = services.SearchIndex;

        var requiresEf =
            Filter?.Foods?.Count > 0 || Filter?.Tools?.Count > 0 || Filter?.Households?.Count > 0 ||
            (Filter?.Tags?.Any(t => Guid.TryParse(t, out _)) ?? false) ||
            (Filter?.Categories?.Any(c => Guid.TryParse(c, out _)) ?? false);

        if (!requiresEf)
        {
            var searchQuery = new RecipeSearchQuery(HouseholdId, Filter?.Search, Filter?.Tags, Filter?.Categories,
                Pagination.Skip, Pagination.PerPage);
            var luceneResult = await searchIndex.SearchAsync(searchQuery, ct);
            var noFilterApplied = Filter is null || Filter.IsEmpty;
            if (luceneResult.Total > 0 || !noFilterApplied)
            {
                var slugs = luceneResult.Slugs;
                var items = await db.Recipes.IgnoreQueryFilters()
                    .Where(r => slugs.Contains(r.Slug))
                    .Include(r => r.Tags).Include(r => r.Categories).ToListAsync(ct);
                var ordered = slugs.Select(s => items.FirstOrDefault(r => r.Slug == s))
                    .Where(r => r is not null).Select(r => RecipeCommandMappings.MapToSummary(r!)).ToList();
                return new PaginatedResponse<RecipeSummaryResponse>
                {
                    Page = Pagination.Page, PerPage = Pagination.PerPage, Total = luceneResult.Total,
                    TotalPages = (int)Math.Ceiling((double)luceneResult.Total / Pagination.PerPage), Items = ordered
                };
            }
        }

        var query = db.Recipes.IgnoreQueryFilters()
            .Where(r => r.HouseholdId == HouseholdId)
            .Include(r => r.Tags).Include(r => r.Categories).AsQueryable();

        if (Filter?.Search is { Length: > 0 } search)
            query = query.Where(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));

        if (Filter?.Tags is { Count: > 0 } tagFilters)
        {
            var tagGuids = tagFilters.Select(t => Guid.TryParse(t, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();
            var tagSlugs = tagFilters.Where(t => !Guid.TryParse(t, out _)).ToList();
            query = Filter.RequireAllTags == true
                ? query.Where(r => tagGuids.All(tid => r.Tags.Any(t => t.Id == tid)) &&
                                   tagSlugs.All(slug => r.Tags.Any(t => t.Slug == slug)))
                : query.Where(r => r.Tags.Any(t => tagGuids.Contains(t.Id) || tagSlugs.Contains(t.Slug)));
        }

        if (Filter?.Categories is { Count: > 0 } catFilters)
        {
            var catGuids = catFilters.Select(c => Guid.TryParse(c, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();
            var catSlugs = catFilters.Where(c => !Guid.TryParse(c, out _)).ToList();
            query = Filter.RequireAllCategories == true
                ? query.Where(r => catGuids.All(cid => r.Categories.Any(c => c.Id == cid)) &&
                                   catSlugs.All(slug => r.Categories.Any(c => c.Slug == slug)))
                : query.Where(r => r.Categories.Any(c => catGuids.Contains(c.Id) || catSlugs.Contains(c.Slug)));
        }

        if (Filter?.Foods is { Count: > 0 } foodFilters)
        {
            var foodGuids = foodFilters.Select(f => Guid.TryParse(f, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();
            query = Filter.RequireAllFoods == true
                ? query.Where(r => foodGuids.All(fid => r.RecipeIngredients.Any(i => i.FoodId == fid)))
                : query.Where(r => r.RecipeIngredients.Any(i => i.FoodId.HasValue && foodGuids.Contains(i.FoodId.Value)));
        }

        if (Filter?.Tools is { Count: > 0 } toolFilters)
        {
            var toolGuids = toolFilters.Select(t => Guid.TryParse(t, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();
            query = Filter.RequireAllTools == true
                ? query.Include(r => r.Tools).Where(r => toolGuids.All(tid => r.Tools.Any(t => t.Id == tid)))
                : query.Include(r => r.Tools).Where(r => r.Tools.Any(t => toolGuids.Contains(t.Id)));
        }

        if (Filter?.Households is { Count: > 0 } householdFilters)
        {
            var hhGuids = householdFilters.Select(h => Guid.TryParse(h, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();
            query = query.Where(r => hhGuids.Contains(r.HouseholdId));
        }

        var total = await query.CountAsync(ct);
        var ordered2 = Filter?.OrderBy?.ToLowerInvariant() switch
        {
            "name" => Filter?.OrderDirection?.ToLowerInvariant() == "asc" ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name),
            "created_at" => Filter?.OrderDirection?.ToLowerInvariant() == "asc" ? query.OrderBy(r => r.CreatedAt) : query.OrderByDescending(r => r.CreatedAt),
            "updated_at" => Filter?.OrderDirection?.ToLowerInvariant() == "asc" ? query.OrderBy(r => r.UpdateAt) : query.OrderByDescending(r => r.UpdateAt),
            "last_made" => Filter?.OrderDirection?.ToLowerInvariant() == "asc" ? query.OrderBy(r => r.LastMade) : query.OrderByDescending(r => r.LastMade),
            "rating" => Filter?.OrderDirection?.ToLowerInvariant() == "asc" ? query.OrderBy(r => r.Rating) : query.OrderByDescending(r => r.Rating),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var efItems = await ordered2.Skip(Pagination.Skip).Take(Pagination.PerPage)
            .Select(r => RecipeCommandMappings.MapToSummary(r)).ToListAsync(ct);
        return new PaginatedResponse<RecipeSummaryResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = efItems
        };
    }
}

public record GetRecipeDetailBySlugQuery(Guid GroupId, string Slug) : IQuery<RecipeDetailResponse?>
{
    public async Task<RecipeDetailResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var r = await services.Db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == GroupId && r.Slug == Slug)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Unit)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Food)
            .Include(r => r.RecipeInstructions).Include(r => r.Notes).Include(r => r.Assets)
            .Include(r => r.Tags).Include(r => r.Categories).Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);
        return r is null ? null : RecipeCommandMappings.MapToDetail(r);
    }
}

public record CreateRecipeCommand(Guid GroupId, Guid HouseholdId, Guid UserId, CreateRecipeRequest Request)
    : IQuery<RecipeDetailResponse>
{
    public async Task<RecipeDetailResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var slug = await RecipeCommandMappings.EnsureUniqueSlugAsync(db, SlugHelper.Generate(Request.Name), ct);
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(), Name = Request.Name, Slug = slug,
            GroupId = GroupId, HouseholdId = HouseholdId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new RecipeCreatedEvent(recipe.Id, HouseholdId), ct);
        return RecipeCommandMappings.MapToDetail(recipe);
    }
}

public record UpdateRecipeCommand(Guid GroupId, string Slug, UpdateRecipeRequest Request) : IQuery<RecipeDetailResponse?>
{
    public async Task<RecipeDetailResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == GroupId && r.Slug == Slug)
            .Include(r => r.RecipeIngredients).Include(r => r.RecipeInstructions)
            .Include(r => r.Notes).Include(r => r.Assets)
            .Include(r => r.Tags).Include(r => r.Categories).Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);
        if (recipe is null) return null;

        if (Request.Name is not null) recipe.Name = Request.Name;
        if (Request.Description is not null) recipe.Description = Request.Description;
        if (Request.RecipeYield is not null) recipe.RecipeYield = Request.RecipeYield;
        if (Request.TotalTime is not null) recipe.TotalTime = Request.TotalTime;
        if (Request.PrepTime is not null) recipe.PrepTime = Request.PrepTime;
        if (Request.CookTime is not null) recipe.CookTime = Request.CookTime;
        if (Request.PerformTime is not null) recipe.PerformTime = Request.PerformTime;
        if (Request.Rating.HasValue) recipe.Rating = Request.Rating;
        if (Request.DisableAmount.HasValue) recipe.DisableAmount = Request.DisableAmount.Value;
        if (Request.OrgUrl is not null) recipe.OrgUrl = Request.OrgUrl;
        if (Request.LastMade.HasValue) recipe.LastMade = Request.LastMade;

        if (Request.Nutrition is not null)
        {
            recipe.Nutrition ??= new Nutrition();
            recipe.Nutrition.Calories = Request.Nutrition.Calories;
            recipe.Nutrition.FatContent = Request.Nutrition.FatContent;
            recipe.Nutrition.ProteinContent = Request.Nutrition.ProteinContent;
            recipe.Nutrition.CarbohydrateContent = Request.Nutrition.CarbohydrateContent;
            recipe.Nutrition.FiberContent = Request.Nutrition.FiberContent;
            recipe.Nutrition.SodiumContent = Request.Nutrition.SodiumContent;
            recipe.Nutrition.SugarContent = Request.Nutrition.SugarContent;
        }

        if (Request.Settings is not null)
        {
            recipe.Settings ??= new RecipeSettings();
            recipe.Settings.Public = Request.Settings.Public;
            recipe.Settings.ShowNutrition = Request.Settings.ShowNutrition;
            recipe.Settings.ShowAssets = Request.Settings.ShowAssets;
            recipe.Settings.LandscapeView = Request.Settings.LandscapeView;
            recipe.Settings.DisableComments = Request.Settings.DisableComments;
            recipe.Settings.DisableAmount = Request.Settings.DisableAmount;
            recipe.Settings.Locked = Request.Settings.Locked;
        }

        if (Request.RecipeIngredients is not null)
        {
            db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            recipe.RecipeIngredients.Clear();
            for (var i = 0; i < Request.RecipeIngredients.Count; i++)
            {
                var ing = Request.RecipeIngredients[i];
                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    Id = ing.Id ?? Guid.NewGuid(), Position = ing.Position, Title = ing.Title,
                    Note = ing.Note, Quantity = ing.Quantity, UnitId = ing.UnitId, FoodId = ing.FoodId,
                    OriginalText = ing.OriginalText, IsFood = ing.IsFood, DisableAmount = ing.DisableAmount,
                    RecipeId = recipe.Id
                });
            }
        }

        if (Request.RecipeInstructions is not null)
        {
            db.RecipeInstructions.RemoveRange(recipe.RecipeInstructions);
            recipe.RecipeInstructions.Clear();
            foreach (var inst in Request.RecipeInstructions)
                recipe.RecipeInstructions.Add(new RecipeInstruction
                {
                    Id = inst.Id ?? Guid.NewGuid(), Position = inst.Position,
                    Text = inst.Text, Title = inst.Title, Summary = inst.Summary, RecipeId = recipe.Id
                });
        }

        if (Request.Notes is not null)
        {
            db.RecipeNotes.RemoveRange(recipe.Notes);
            recipe.Notes.Clear();
            foreach (var note in Request.Notes)
                recipe.Notes.Add(new RecipeNote
                    { Id = note.Id ?? Guid.NewGuid(), Title = note.Title, Text = note.Text, RecipeId = recipe.Id });
        }

        if (Request.Tags is not null)
        {
            recipe.Tags.Clear();
            foreach (var tagSlug in Request.Tags)
            {
                var tag = await db.Tags.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Slug == tagSlug && t.GroupId == GroupId, ct);
                if (tag is not null) recipe.Tags.Add(tag);
            }
        }

        if (Request.Categories is not null)
        {
            recipe.Categories.Clear();
            foreach (var catSlug in Request.Categories)
            {
                var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == GroupId, ct);
                if (cat is not null) recipe.Categories.Add(cat);
            }
        }

        if (Request.Tools is not null)
        {
            recipe.Tools.Clear();
            foreach (var toolSlug in Request.Tools)
            {
                var tool = await db.Tools.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Slug == toolSlug && t.GroupId == GroupId, ct);
                if (tool is not null) recipe.Tools.Add(tool);
            }
        }

        recipe.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new RecipeUpdatedEvent(recipe.Id, recipe.HouseholdId), ct);

        foreach (var ing in recipe.RecipeIngredients)
        {
            if (ing.UnitId.HasValue && ing.Unit is null) await db.Entry(ing).Reference(i => i.Unit).LoadAsync(ct);
            if (ing.FoodId.HasValue && ing.Food is null) await db.Entry(ing).Reference(i => i.Food).LoadAsync(ct);
        }

        return RecipeCommandMappings.MapToDetail(recipe);
    }
}

public record DeleteRecipeCommand(Guid GroupId, string Slug) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipe = await db.Recipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.GroupId == GroupId && r.Slug == Slug, ct);
        if (recipe is null) return false;
        var recipeId = recipe.Id;
        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new RecipeDeletedEvent(recipeId, recipe.HouseholdId), ct);
        return true;
    }
}

public record DuplicateRecipeCommand(Guid GroupId, Guid HouseholdId, Guid UserId, string Slug)
    : IQuery<RecipeDetailResponse?>
{
    public async Task<RecipeDetailResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var original = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == GroupId && r.Slug == Slug)
            .Include(r => r.RecipeIngredients).Include(r => r.RecipeInstructions)
            .Include(r => r.Notes).Include(r => r.Tags).Include(r => r.Categories).Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);
        if (original is null) return null;

        var newName = $"{original.Name} (Copy)";
        var newSlug = await RecipeCommandMappings.EnsureUniqueSlugAsync(db, SlugHelper.Generate(newName), ct);
        var copy = new Recipe
        {
            Id = Guid.NewGuid(), Name = newName, Slug = newSlug, Description = original.Description,
            RecipeYield = original.RecipeYield, TotalTime = original.TotalTime, PrepTime = original.PrepTime,
            CookTime = original.CookTime, PerformTime = original.PerformTime, Rating = original.Rating,
            DisableAmount = original.DisableAmount, Image = original.Image, OrgUrl = original.OrgUrl,
            GroupId = GroupId, HouseholdId = HouseholdId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };

        foreach (var ing in original.RecipeIngredients)
            copy.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(), Position = ing.Position, Title = ing.Title, Note = ing.Note,
                Quantity = ing.Quantity, UnitId = ing.UnitId, FoodId = ing.FoodId,
                OriginalText = ing.OriginalText, IsFood = ing.IsFood, DisableAmount = ing.DisableAmount, RecipeId = copy.Id
            });
        foreach (var inst in original.RecipeInstructions)
            copy.RecipeInstructions.Add(new RecipeInstruction
                { Id = Guid.NewGuid(), Position = inst.Position, Text = inst.Text, Title = inst.Title, Summary = inst.Summary, RecipeId = copy.Id });
        foreach (var note in original.Notes)
            copy.Notes.Add(new RecipeNote { Id = Guid.NewGuid(), Title = note.Title, Text = note.Text, RecipeId = copy.Id });
        foreach (var tag in original.Tags) copy.Tags.Add(tag);
        foreach (var cat in original.Categories) copy.Categories.Add(cat);
        foreach (var tool in original.Tools) copy.Tools.Add(tool);

        db.Recipes.Add(copy);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new RecipeCreatedEvent(copy.Id, copy.HouseholdId), ct);
        return RecipeCommandMappings.MapToDetail(copy);
    }
}

public record CreateRecipeFromScrapedCommand(
    ScrapedRecipeDto Scraped, Guid HouseholdId, Guid GroupId,
    IReadOnlyList<ParsedIngredientResult>? ParsedIngredients = null,
    List<IngredientFood>? CachedFoods = null, List<IngredientUnit>? CachedUnits = null)
    : IQuery<RecipeSummaryResponse?>
{
    public async Task<RecipeSummaryResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var logger = services.LoggerFactory.CreateLogger("RecipeCommands");
        var slug = SlugHelper.Generate(Scraped.Name ?? "untitled");
        var existingSlug = await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.HouseholdId == HouseholdId && r.Slug == slug, ct);
        if (existingSlug) return null;

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

        var parsed = ParsedIngredients ?? await services.IngredientParser.ParseBatchAsync(ingredientStrings, ct);

        var foods = CachedFoods ?? await db.Foods.IgnoreQueryFilters()
            .Where(f => f.GroupId == GroupId).Include(f => f.Aliases).ToListAsync(ct);
        var units = CachedUnits ?? await db.Units.IgnoreQueryFilters()
            .Where(u => u.GroupId == GroupId).ToListAsync(ct);

        for (var i = 0; i < parsed.Count; i++)
        {
            var p = parsed[i];
            IngredientFood? matchedFood = null;
            if (!string.IsNullOrWhiteSpace(p.Food))
            {
                matchedFood = foods.FirstOrDefault(f =>
                    string.Equals(f.Name, p.Food, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.PluralName, p.Food, StringComparison.OrdinalIgnoreCase) ||
                    f.Aliases.Any(a => string.Equals(a.Name, p.Food, StringComparison.OrdinalIgnoreCase)));
                if (matchedFood is null)
                {
                    matchedFood = new IngredientFood
                    {
                        Id = Guid.NewGuid(), Name = p.Food, GroupId = GroupId,
                        CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                    };
                    db.Foods.Add(matchedFood);
                    foods.Add(matchedFood);
                }
            }

            IngredientUnit? matchedUnit = null;
            if (!string.IsNullOrWhiteSpace(p.Unit))
            {
                matchedUnit = units.FirstOrDefault(u =>
                    string.Equals(u.Name, p.Unit, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.PluralName, p.Unit, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.Abbreviation, p.Unit, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.PluralAbbreviation, p.Unit, StringComparison.OrdinalIgnoreCase));
                if (matchedUnit is null)
                {
                    matchedUnit = new IngredientUnit
                    {
                        Id = Guid.NewGuid(), Name = p.Unit, GroupId = GroupId,
                        CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                    };
                    db.Units.Add(matchedUnit);
                    units.Add(matchedUnit);
                }
            }

            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(), Position = i, OriginalText = p.Input, Note = p.Note,
                Quantity = p.Quantity.HasValue ? (decimal?)p.Quantity.Value : null,
                FoodId = matchedFood?.Id, UnitId = matchedUnit?.Id, RecipeId = recipe.Id
            });
        }

        for (var i = 0; i < Scraped.RecipeInstructions.Count; i++)
            recipe.RecipeInstructions.Add(new RecipeInstruction
                { Id = Guid.NewGuid(), Position = i, Text = Scraped.RecipeInstructions[i], RecipeId = recipe.Id });

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct);

        foreach (var keyword in Scraped.Keywords)
        {
            var tagName = keyword.Trim();
            if (string.IsNullOrEmpty(tagName)) continue;
            var tagSlug = SlugHelper.Generate(tagName);
            var tag = await db.Tags.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Slug == tagSlug && t.GroupId == GroupId, ct);
            if (tag is null)
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = tagName, Slug = tagSlug, GroupId = GroupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
                db.Tags.Add(tag);
                await db.SaveChangesAsync(ct);
            }
            recipe.Tags.Add(tag);
        }

        foreach (var catName in Scraped.Categories)
        {
            var name = catName.Trim();
            if (string.IsNullOrEmpty(name)) continue;
            var catSlug = SlugHelper.Generate(name);
            var cat = await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == GroupId, ct);
            if (cat is null)
            {
                cat = new Category { Id = Guid.NewGuid(), Name = name, Slug = catSlug, GroupId = GroupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
                db.Categories.Add(cat);
                await db.SaveChangesAsync(ct);
            }
            recipe.Categories.Add(cat);
        }

        if (Scraped.Keywords.Any() || Scraped.Categories.Any())
            await db.SaveChangesAsync(ct);

        var hasDirectImage = !string.IsNullOrEmpty(Scraped.Image);
        var hasOrgUrl = !string.IsNullOrEmpty(Scraped.OrgUrl);
        logger.LogInformation("Image queue check for recipe {RecipeId}: hasDirectImage={HasDirectImage}, hasOrgUrl={HasOrgUrl}",
            recipe.Id, hasDirectImage, hasOrgUrl);
        if (hasDirectImage || hasOrgUrl)
        {
            logger.LogInformation("Queuing image scrape job for recipe {RecipeId}", recipe.Id);
            await services.ImageScrapeQueue.Writer.WriteAsync(new ImageScrapeJob(
                recipe.Id, hasOrgUrl ? Scraped.OrgUrl : null, hasDirectImage ? Scraped.Image : null), CancellationToken.None);
        }

        await services.Mediator.Publish(new RecipeCreatedEvent(recipe.Id, HouseholdId), CancellationToken.None);
        return RecipeCommandMappings.MapToSummary(recipe);
    }
}

public record BulkDeleteRecipesCommand(IList<string> Slugs) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipes = await db.Recipes.Where(r => Slugs.Contains(r.Slug)).ToListAsync(ct);
        var recipeIds = recipes.Select(r => r.Id).ToList();
        db.Recipes.RemoveRange(recipes);
        await db.SaveChangesAsync(ct);
        if (recipeIds.Count > 0)
            await services.Mediator.Publish(new RecipesBulkDeletedEvent(recipeIds), ct);
        return true;
    }
}

public record BulkTagRecipesCommand(IList<string> Slugs, IList<string> TagNames, Guid GroupId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipes = await db.Recipes.Include(r => r.Tags).Where(r => Slugs.Contains(r.Slug)).ToListAsync(ct);
        foreach (var tagName in TagNames)
        {
            var tagSlug = SlugHelper.Generate(tagName);
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Slug == tagSlug && t.GroupId == GroupId, ct)
                      ?? new Tag { Id = Guid.NewGuid(), Name = tagName, Slug = tagSlug, GroupId = GroupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
            if (tag.Id == Guid.Empty || !db.Tags.Local.Contains(tag)) db.Tags.Add(tag);
            foreach (var recipe in recipes)
                if (!recipe.Tags.Any(t => t.Slug == tagSlug)) recipe.Tags.Add(tag);
        }
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record BulkCategorizeRecipesCommand(IList<string> Slugs, IList<string> CategoryNames, Guid GroupId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipes = await db.Recipes.Include(r => r.Categories).Where(r => Slugs.Contains(r.Slug)).ToListAsync(ct);
        foreach (var catName in CategoryNames)
        {
            var catSlug = SlugHelper.Generate(catName);
            var cat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == GroupId, ct)
                      ?? new Category { Id = Guid.NewGuid(), Name = catName, Slug = catSlug, GroupId = GroupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
            if (cat.Id == Guid.Empty || !db.Categories.Local.Contains(cat)) db.Categories.Add(cat);
            foreach (var recipe in recipes)
                if (!recipe.Categories.Any(c => c.Slug == catSlug)) recipe.Categories.Add(cat);
        }
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record GetRecipeSuggestionsQuery(
    Guid HouseholdId, Guid GroupId, int Limit, string? QueryFilter,
    int MaxMissingFoods, int MaxMissingTools, bool IncludeFoodsOnHand, bool IncludeToolsOnHand,
    IList<Guid> FoodIds, IList<Guid> ToolIds)
    : IQuery<RecipeSuggestionsResponse>
{
    public async Task<RecipeSuggestionsResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var selectedFoodIds = new HashSet<Guid>(FoodIds);
        var selectedToolIds = new HashSet<Guid>(ToolIds);

        if (IncludeFoodsOnHand)
        {
            var onHand = await db.Foods.IgnoreQueryFilters()
                .Where(f => f.GroupId == GroupId && f.OnHand).Select(f => f.Id).ToListAsync(ct);
            foreach (var id in onHand) selectedFoodIds.Add(id);
        }
        if (IncludeToolsOnHand)
        {
            var onHand = await db.Tools.IgnoreQueryFilters()
                .Where(t => t.GroupId == GroupId && t.OnHand).Select(t => t.Id).ToListAsync(ct);
            foreach (var id in onHand) selectedToolIds.Add(id);
        }

        var recipes = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.HouseholdId == HouseholdId)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Food)
            .Include(r => r.Tools).Include(r => r.Tags).Include(r => r.Categories).ToListAsync(ct);

        var suggestions = new List<RecipeSuggestionItem>();
        foreach (var recipe in recipes)
        {
            var recipeFoodIds = recipe.RecipeIngredients.Where(i => i.FoodId.HasValue).Select(i => i.FoodId!.Value).Distinct().ToHashSet();
            var recipeToolIds = recipe.Tools.Select(t => t.Id).Distinct().ToHashSet();
            var missingFoods = recipeFoodIds.Where(fid => !selectedFoodIds.Contains(fid)).ToList();
            var missingTools = recipeToolIds.Where(tid => !selectedToolIds.Contains(tid)).ToList();
            if (missingFoods.Count > MaxMissingFoods || missingTools.Count > MaxMissingTools) continue;
            if (FoodIds.Count > 0 && !recipeFoodIds.Any(fid => FoodIds.Contains(fid))) continue;
            if (!string.IsNullOrWhiteSpace(QueryFilter))
            {
                var q = QueryFilter.ToLowerInvariant();
                if (!recipe.Name.ToLowerInvariant().Contains(q) && !(recipe.Description?.ToLowerInvariant().Contains(q) ?? false)) continue;
            }

            var missingFoodDetails = await db.Foods.IgnoreQueryFilters()
                .Where(f => missingFoods.Contains(f.Id))
                .Select(f => new RecipeIngredientFoodDto { Id = f.Id, Name = f.Name, PluralName = f.PluralName, Description = f.Description })
                .ToListAsync(ct);
            var missingToolDetails = await db.Tools.IgnoreQueryFilters()
                .Where(t => missingTools.Contains(t.Id))
                .Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug })
                .ToListAsync(ct);

            suggestions.Add(new RecipeSuggestionItem { Recipe = RecipeCommandMappings.MapToSummary(recipe), MissingFoods = missingFoodDetails, MissingTools = missingToolDetails });
        }

        var result = suggestions
            .OrderBy(s => s.MissingTools.Count).ThenBy(s => s.MissingFoods.Count)
            .ThenByDescending(s =>
            {
                var rFoodIds = recipes.First(r => r.Id == s.Recipe.Id)
                    .RecipeIngredients.Where(i => i.IsFood && i.FoodId.HasValue).Select(i => i.FoodId!.Value).ToHashSet();
                return rFoodIds.Count(fid => FoodIds.Contains(fid));
            })
            .Take(Limit).ToList();

        return new RecipeSuggestionsResponse { Items = result };
    }
}

file static class RecipeCommandMappings
{
    public static async Task<string> EnsureUniqueSlugAsync(ApplicationDbContext db, string slug, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Slug == candidate, ct))
            candidate = $"{slug}-{counter++}";
        return candidate;
    }

    public static RecipeSummaryResponse MapToSummary(Recipe r) =>
        new()
        {
            Id = r.Id, Name = r.Name, Slug = r.Slug, Description = r.Description,
            Image = r.Image, OrgUrl = r.OrgUrl, Rating = r.Rating,
            GroupId = r.GroupId, HouseholdId = r.HouseholdId, CreatedAt = r.CreatedAt, UpdateAt = r.UpdateAt,
            Tags = r.Tags.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
            Categories = r.Categories.Select(c => new OrganizerSimpleResponse { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList()
        };

    public static RecipeDetailResponse MapToDetail(Recipe r) =>
        new()
        {
            Id = r.Id, Name = r.Name, Slug = r.Slug, Description = r.Description,
            RecipeYield = r.RecipeYield, TotalTime = r.TotalTime, PrepTime = r.PrepTime,
            CookTime = r.CookTime, PerformTime = r.PerformTime, Rating = r.Rating,
            DisableAmount = r.DisableAmount, Image = r.Image, OrgUrl = r.OrgUrl,
            GroupId = r.GroupId, HouseholdId = r.HouseholdId, CreatedAt = r.CreatedAt, UpdateAt = r.UpdateAt, LastMade = r.LastMade,
            Nutrition = r.Nutrition is null ? null : new NutritionDto
            {
                Calories = r.Nutrition.Calories, FatContent = r.Nutrition.FatContent,
                ProteinContent = r.Nutrition.ProteinContent, CarbohydrateContent = r.Nutrition.CarbohydrateContent,
                FiberContent = r.Nutrition.FiberContent, SodiumContent = r.Nutrition.SodiumContent, SugarContent = r.Nutrition.SugarContent
            },
            Settings = new RecipeSettingsDto
            {
                Public = r.Settings?.Public ?? false, ShowNutrition = r.Settings?.ShowNutrition ?? false,
                ShowAssets = r.Settings?.ShowAssets ?? false, LandscapeView = r.Settings?.LandscapeView ?? false,
                DisableComments = r.Settings?.DisableComments ?? false, DisableAmount = r.Settings?.DisableAmount ?? false,
                Locked = r.Settings?.Locked ?? false
            },
            RecipeIngredients = r.RecipeIngredients.Select(i => new RecipeIngredientDto
            {
                Id = i.Id, Position = i.Position, Title = i.Title, Note = i.Note,
                Quantity = i.Quantity, OriginalText = i.OriginalText, IsFood = i.IsFood, DisableAmount = i.DisableAmount,
                Unit = i.Unit is null ? null : new RecipeIngredientUnitDto { Id = i.Unit.Id, Name = i.Unit.Name, Abbreviation = i.Unit.Abbreviation },
                Food = i.Food is null ? null : new RecipeIngredientFoodDto { Id = i.Food.Id, Name = i.Food.Name }
            }).ToList(),
            RecipeInstructions = r.RecipeInstructions.Select(i => new RecipeInstructionDto
                { Id = i.Id, Position = i.Position, Text = i.Text, Title = i.Title, Summary = i.Summary }).ToList(),
            Notes = r.Notes.Select(n => new RecipeNoteDto { Id = n.Id, Title = n.Title, Text = n.Text }).ToList(),
            Assets = r.Assets.Select(a => new RecipeAssetDto { Id = a.Id, Name = a.Name, Icon = a.Icon, FileName = $"{a.Name}.{a.Extension}" }).ToList(),
            Tags = r.Tags.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
            Categories = r.Categories.Select(c => new OrganizerSimpleResponse { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList(),
            Tools = r.Tools.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList()
        };
}
