using Mealie.Application.Common;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.IngredientParser;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Entities.Organizers;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Mealie.Infrastructure.Scraper;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Recipes;

public class RecipeService(ApplicationDbContext db, IngredientParserService ingredientParser) : IRecipeService
{
    public async Task<IList<RecipeSummaryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Recipes
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Select(r => MapToSummary(r))
            .ToListAsync(ct);
    }

    public async Task<PaginatedResponse<RecipeSummaryResponse>> GetPaginatedAsync(
        Guid householdId, PaginationParams pagination, RecipeFilter? filter = null, CancellationToken ct = default)
    {
        var query = db.Recipes.IgnoreQueryFilters()
            .Where(r => r.HouseholdId == householdId)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .AsQueryable();

        if (filter?.Search is { Length: > 0 } search)
            query = query.Where(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));

        if (filter?.Tags is { Count: > 0 } tagFilters)
        {
            var tagGuids = tagFilters
                .Select(t => Guid.TryParse(t, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();
            var tagSlugs = tagFilters.Where(t => !Guid.TryParse(t, out _)).ToList();
            query = query.Where(r => r.Tags.Any(t => tagGuids.Contains(t.Id) || tagSlugs.Contains(t.Slug)));
        }

        if (filter?.Categories is { Count: > 0 } catFilters)
        {
            var catGuids = catFilters
                .Select(c => Guid.TryParse(c, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue).Select(g => g!.Value).ToList();
            var catSlugs = catFilters.Where(c => !Guid.TryParse(c, out _)).ToList();
            query = query.Where(r => r.Categories.Any(c => catGuids.Contains(c.Id) || catSlugs.Contains(c.Slug)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(r => r.Name)
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .Select(r => MapToSummary(r))
            .ToListAsync(ct);

        return new PaginatedResponse<RecipeSummaryResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items
        };
    }

    public async Task<RecipeDetailResponse?> GetDetailBySlugAsync(Guid groupId, string slug, CancellationToken ct = default)
    {
        var r = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == groupId && r.Slug == slug)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Unit)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Food)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Assets)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);

        if (r is null) return null;
        return MapToDetail(r);
    }

    public async Task<RecipeSummaryResponse?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var r = await db.Recipes
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Slug == slug, ct);
        if (r is null) return null;
        return MapToSummary(r);
    }

    public async Task<RecipeDetailResponse> CreateAsync(
        Guid groupId, Guid householdId, Guid userId, CreateRecipeRequest request, CancellationToken ct = default)
    {
        var baseSlug = SlugHelper.Generate(request.Name);
        var slug = await EnsureUniqueSlugAsync(baseSlug, ct);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            GroupId = groupId,
            HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
        };

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct);
        return MapToDetail(recipe);
    }

    public async Task<RecipeDetailResponse?> UpdateAsync(
        Guid groupId, string slug, UpdateRecipeRequest request, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == groupId && r.Slug == slug)
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Assets)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);

        if (recipe is null) return null;

        if (request.Name is not null) recipe.Name = request.Name;
        if (request.Description is not null) recipe.Description = request.Description;
        if (request.RecipeYield is not null) recipe.RecipeYield = request.RecipeYield;
        if (request.TotalTime is not null) recipe.TotalTime = request.TotalTime;
        if (request.PrepTime is not null) recipe.PrepTime = request.PrepTime;
        if (request.CookTime is not null) recipe.CookTime = request.CookTime;
        if (request.PerformTime is not null) recipe.PerformTime = request.PerformTime;
        if (request.Rating.HasValue) recipe.Rating = request.Rating;
        if (request.DisableAmount.HasValue) recipe.DisableAmount = request.DisableAmount.Value;
        if (request.OrgUrl is not null) recipe.OrgUrl = request.OrgUrl;
        if (request.LastMade.HasValue) recipe.LastMade = request.LastMade;

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

        if (request.RecipeIngredients is not null)
        {
            db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            recipe.RecipeIngredients.Clear();
            for (var i = 0; i < request.RecipeIngredients.Count; i++)
            {
                var ing = request.RecipeIngredients[i];
                recipe.RecipeIngredients.Add(new RecipeIngredient
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
                });
            }
        }

        if (request.RecipeInstructions is not null)
        {
            db.RecipeInstructions.RemoveRange(recipe.RecipeInstructions);
            recipe.RecipeInstructions.Clear();
            foreach (var inst in request.RecipeInstructions)
            {
                recipe.RecipeInstructions.Add(new RecipeInstruction
                {
                    Id = inst.Id ?? Guid.NewGuid(),
                    Position = inst.Position,
                    Text = inst.Text,
                    Title = inst.Title,
                    Summary = inst.Summary,
                    RecipeId = recipe.Id
                });
            }
        }

        if (request.Notes is not null)
        {
            db.RecipeNotes.RemoveRange(recipe.Notes);
            recipe.Notes.Clear();
            foreach (var note in request.Notes)
            {
                recipe.Notes.Add(new RecipeNote
                {
                    Id = note.Id ?? Guid.NewGuid(),
                    Title = note.Title,
                    Text = note.Text,
                    RecipeId = recipe.Id
                });
            }
        }

        if (request.Tags is not null)
        {
            recipe.Tags.Clear();
            foreach (var tagSlug in request.Tags)
            {
                var tag = await db.Tags.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == tagSlug && t.GroupId == groupId, ct);
                if (tag is not null) recipe.Tags.Add(tag);
            }
        }

        if (request.Categories is not null)
        {
            recipe.Categories.Clear();
            foreach (var catSlug in request.Categories)
            {
                var cat = await db.Categories.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == groupId, ct);
                if (cat is not null) recipe.Categories.Add(cat);
            }
        }

        if (request.Tools is not null)
        {
            recipe.Tools.Clear();
            foreach (var toolSlug in request.Tools)
            {
                var tool = await db.Tools.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == toolSlug && t.GroupId == groupId, ct);
                if (tool is not null) recipe.Tools.Add(tool);
            }
        }

        recipe.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToDetail(recipe);
    }

    public async Task<bool> DeleteAsync(Guid groupId, string slug, CancellationToken ct = default)
    {
        var recipe = await db.Recipes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.GroupId == groupId && r.Slug == slug, ct);
        if (recipe is null) return false;
        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<RecipeDetailResponse?> DuplicateAsync(
        Guid groupId, Guid householdId, Guid userId, string slug, CancellationToken ct = default)
    {
        var original = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.GroupId == groupId && r.Slug == slug)
            .Include(r => r.RecipeIngredients)
            .Include(r => r.RecipeInstructions)
            .Include(r => r.Notes)
            .Include(r => r.Tags)
            .Include(r => r.Categories)
            .Include(r => r.Tools)
            .FirstOrDefaultAsync(ct);

        if (original is null) return null;

        var newName = $"{original.Name} (Copy)";
        var baseSlug = SlugHelper.Generate(newName);
        var newSlug = await EnsureUniqueSlugAsync(baseSlug, ct);

        var copy = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = newName,
            Slug = newSlug,
            Description = original.Description,
            RecipeYield = original.RecipeYield,
            TotalTime = original.TotalTime,
            PrepTime = original.PrepTime,
            CookTime = original.CookTime,
            PerformTime = original.PerformTime,
            Rating = original.Rating,
            DisableAmount = original.DisableAmount,
            Image = original.Image,
            OrgUrl = original.OrgUrl,
            GroupId = groupId,
            HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
        };

        foreach (var ing in original.RecipeIngredients)
            copy.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(), Position = ing.Position, Title = ing.Title,
                Note = ing.Note, Quantity = ing.Quantity, UnitId = ing.UnitId,
                FoodId = ing.FoodId, OriginalText = ing.OriginalText,
                IsFood = ing.IsFood, DisableAmount = ing.DisableAmount, RecipeId = copy.Id
            });

        foreach (var inst in original.RecipeInstructions)
            copy.RecipeInstructions.Add(new RecipeInstruction
            {
                Id = Guid.NewGuid(), Position = inst.Position, Text = inst.Text,
                Title = inst.Title, Summary = inst.Summary, RecipeId = copy.Id
            });

        foreach (var note in original.Notes)
            copy.Notes.Add(new RecipeNote
            { Id = Guid.NewGuid(), Title = note.Title, Text = note.Text, RecipeId = copy.Id });

        foreach (var tag in original.Tags) copy.Tags.Add(tag);
        foreach (var cat in original.Categories) copy.Categories.Add(cat);
        foreach (var tool in original.Tools) copy.Tools.Add(tool);

        db.Recipes.Add(copy);
        await db.SaveChangesAsync(ct);
        return MapToDetail(copy);
    }

    public async Task<RecipeSummaryResponse?> CreateFromScrapedAsync(
        ScrapedRecipeDto scraped, Guid householdId, Guid groupId,
        IReadOnlyList<ParsedIngredientResult>? parsedIngredients = null,
        List<IngredientFood>? cachedFoods = null,
        List<IngredientUnit>? cachedUnits = null,
        CancellationToken ct = default)
    {
        var slug = SlugHelper.Generate(scraped.Name ?? "untitled");

        // If this recipe already exists (e.g. migration resumed after restart), skip it.
        var existingSlug = await db.Recipes.IgnoreQueryFilters()
            .AnyAsync(r => r.HouseholdId == householdId && r.Slug == slug, ct);
        if (existingSlug)
            return null;

        var uniqueSlug = await EnsureUniqueSlugAsync(slug, ct);

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = scraped.Name ?? "Untitled Recipe",
            Slug = uniqueSlug,
            Description = scraped.Description,
            Image = scraped.Image,
            RecipeYield = scraped.RecipeYield,
            TotalTime = scraped.TotalTime,
            PrepTime = scraped.PrepTime,
            CookTime = scraped.CookTime,
            GroupId = groupId,
            HouseholdId = householdId,
            OrgUrl = scraped.OrgUrl,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow,
        };

        // Use pre-parsed results when provided (batch migration path); otherwise parse now.
        var parsed = parsedIngredients
            ?? await ingredientParser.ParseBatchAsync(scraped.RecipeIngredient, ct);

        // Use caller-provided mutable lists (migration batch path) or load fresh from DB.
        // Mutable so newly created foods/units are visible to subsequent ingredients in the same batch.
        var foods = cachedFoods
            ?? await db.Foods.IgnoreQueryFilters()
                .Where(f => f.GroupId == groupId)
                .Include(f => f.Aliases)
                .ToListAsync(ct);

        var units = cachedUnits
            ?? await db.Units.IgnoreQueryFilters()
                .Where(u => u.GroupId == groupId)
                .ToListAsync(ct);

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

                // Create food on first encounter so subsequent ingredients in the batch can match it.
                if (matchedFood is null)
                {
                    matchedFood = new IngredientFood
                    {
                        Id = Guid.NewGuid(),
                        Name = p.Food,
                        GroupId = groupId,
                        CreatedAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow,
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

                // Create unit on first encounter.
                if (matchedUnit is null)
                {
                    matchedUnit = new IngredientUnit
                    {
                        Id = Guid.NewGuid(),
                        Name = p.Unit,
                        GroupId = groupId,
                        CreatedAt = DateTime.UtcNow,
                        UpdateAt = DateTime.UtcNow,
                    };
                    db.Units.Add(matchedUnit);
                    units.Add(matchedUnit);
                }
            }

            recipe.RecipeIngredients.Add(new RecipeIngredient
            {
                Id = Guid.NewGuid(),
                Position = i,
                OriginalText = p.Input,
                Note = p.Note,
                Quantity = p.Quantity.HasValue ? (decimal?)p.Quantity.Value : null,
                FoodId = matchedFood?.Id,
                UnitId = matchedUnit?.Id,
                RecipeId = recipe.Id,
            });
        }

        for (var i = 0; i < scraped.RecipeInstructions.Count; i++)
        {
            recipe.RecipeInstructions.Add(new RecipeInstruction
            {
                Id = Guid.NewGuid(),
                Position = i,
                Text = scraped.RecipeInstructions[i],
                RecipeId = recipe.Id,
            });
        }

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct);

        // Upsert and assign tags from keywords
        foreach (var keyword in scraped.Keywords)
        {
            var tagName = keyword.Trim();
            if (string.IsNullOrEmpty(tagName)) continue;
            var tagSlug = SlugHelper.Generate(tagName);
            var tag = await db.Tags.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == tagSlug && t.GroupId == groupId, ct);
            if (tag is null)
            {
                tag = new Tag { Id = Guid.NewGuid(), Name = tagName, Slug = tagSlug, GroupId = groupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
                db.Tags.Add(tag);
                await db.SaveChangesAsync(ct);
            }
            recipe.Tags.Add(tag);
        }

        // Upsert and assign categories
        foreach (var catName in scraped.Categories)
        {
            var name = catName.Trim();
            if (string.IsNullOrEmpty(name)) continue;
            var catSlug = SlugHelper.Generate(name);
            var cat = await db.Categories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == groupId, ct);
            if (cat is null)
            {
                cat = new Category { Id = Guid.NewGuid(), Name = name, Slug = catSlug, GroupId = groupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
                db.Categories.Add(cat);
                await db.SaveChangesAsync(ct);
            }
            recipe.Categories.Add(cat);
        }

        if (scraped.Keywords.Any() || scraped.Categories.Any())
            await db.SaveChangesAsync(ct);

        return MapToSummary(recipe);
    }

    public async Task BulkDeleteAsync(IList<string> slugs, CancellationToken ct = default)
    {
        var recipes = await db.Recipes
            .Where(r => slugs.Contains(r.Slug))
            .ToListAsync(ct);
        db.Recipes.RemoveRange(recipes);
        await db.SaveChangesAsync(ct);
    }

    public async Task BulkTagAsync(IList<string> slugs, IList<string> tagNames, Guid groupId, CancellationToken ct = default)
    {
        var recipes = await db.Recipes
            .Include(r => r.Tags)
            .Where(r => slugs.Contains(r.Slug))
            .ToListAsync(ct);

        foreach (var tagName in tagNames)
        {
            var tagSlug = SlugHelper.Generate(tagName);
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Slug == tagSlug && t.GroupId == groupId, ct)
                ?? new Tag { Id = Guid.NewGuid(), Name = tagName, Slug = tagSlug, GroupId = groupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };

            if (tag.Id == Guid.Empty || !db.Tags.Local.Contains(tag))
                db.Tags.Add(tag);

            foreach (var recipe in recipes)
            {
                if (!recipe.Tags.Any(t => t.Slug == tagSlug))
                    recipe.Tags.Add(tag);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task BulkCategorizeAsync(IList<string> slugs, IList<string> categoryNames, Guid groupId, CancellationToken ct = default)
    {
        var recipes = await db.Recipes
            .Include(r => r.Categories)
            .Where(r => slugs.Contains(r.Slug))
            .ToListAsync(ct);

        foreach (var catName in categoryNames)
        {
            var catSlug = SlugHelper.Generate(catName);
            var cat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == groupId, ct)
                ?? new Category { Id = Guid.NewGuid(), Name = catName, Slug = catSlug, GroupId = groupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };

            if (cat.Id == Guid.Empty || !db.Categories.Local.Contains(cat))
                db.Categories.Add(cat);

            foreach (var recipe in recipes)
            {
                if (!recipe.Categories.Any(c => c.Slug == catSlug))
                    recipe.Categories.Add(cat);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, CancellationToken ct)
    {
        var candidate = slug;
        var counter = 1;
        while (await db.Recipes.IgnoreQueryFilters().AnyAsync(r => r.Slug == candidate, ct))
            candidate = $"{slug}-{counter++}";
        return candidate;
    }

    private static RecipeSummaryResponse MapToSummary(Recipe r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Slug = r.Slug,
        Description = r.Description,
        Image = r.Image,
        OrgUrl = r.OrgUrl,
        Rating = r.Rating,
        GroupId = r.GroupId,
        HouseholdId = r.HouseholdId,
        CreatedAt = r.CreatedAt,
        UpdateAt = r.UpdateAt,
        Tags = r.Tags.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
        Categories = r.Categories.Select(c => new OrganizerSimpleResponse { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList(),
    };

    private static RecipeDetailResponse MapToDetail(Recipe r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Slug = r.Slug,
        Description = r.Description,
        RecipeYield = r.RecipeYield,
        TotalTime = r.TotalTime,
        PrepTime = r.PrepTime,
        CookTime = r.CookTime,
        PerformTime = r.PerformTime,
        Rating = r.Rating,
        DisableAmount = r.DisableAmount,
        Image = r.Image,
        OrgUrl = r.OrgUrl,
        GroupId = r.GroupId,
        HouseholdId = r.HouseholdId,
        CreatedAt = r.CreatedAt,
        UpdateAt = r.UpdateAt,
        LastMade = r.LastMade,
        Nutrition = r.Nutrition is null ? null : new Mealie.Application.Dtos.Recipes.NutritionDto
        {
            Calories = r.Nutrition.Calories,
            FatContent = r.Nutrition.FatContent,
            ProteinContent = r.Nutrition.ProteinContent,
            CarbohydrateContent = r.Nutrition.CarbohydrateContent,
            FiberContent = r.Nutrition.FiberContent,
            SodiumContent = r.Nutrition.SodiumContent,
            SugarContent = r.Nutrition.SugarContent,
        },
        Settings = new RecipeSettingsDto
        {
            Public = r.Settings?.Public ?? false,
            ShowNutrition = r.Settings?.ShowNutrition ?? false,
            ShowAssets = r.Settings?.ShowAssets ?? false,
            LandscapeView = r.Settings?.LandscapeView ?? false,
            DisableComments = r.Settings?.DisableComments ?? false,
            DisableAmount = r.Settings?.DisableAmount ?? false,
            Locked = r.Settings?.Locked ?? false,
        },
        RecipeIngredients = r.RecipeIngredients.Select(i => new RecipeIngredientDto
        {
            Id = i.Id, Position = i.Position, Title = i.Title, Note = i.Note,
            Quantity = i.Quantity, OriginalText = i.OriginalText, IsFood = i.IsFood, DisableAmount = i.DisableAmount,
            Unit = i.Unit is null ? null : new RecipeIngredientUnitDto { Id = i.Unit.Id, Name = i.Unit.Name, Abbreviation = i.Unit.Abbreviation },
            Food = i.Food is null ? null : new RecipeIngredientFoodDto { Id = i.Food.Id, Name = i.Food.Name },
        }).ToList(),
        RecipeInstructions = r.RecipeInstructions.Select(i => new RecipeInstructionDto
        {
            Id = i.Id, Position = i.Position, Text = i.Text, Title = i.Title, Summary = i.Summary,
        }).ToList(),
        Notes = r.Notes.Select(n => new RecipeNoteDto { Id = n.Id, Title = n.Title, Text = n.Text }).ToList(),
        Assets = r.Assets.Select(a => new RecipeAssetDto { Id = a.Id, Name = a.Name, Icon = a.Icon, FileName = $"{a.Name}.{a.Extension}" }).ToList(),
        Tags = r.Tags.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
        Categories = r.Categories.Select(c => new OrganizerSimpleResponse { Id = c.Id, Name = c.Name, Slug = c.Slug }).ToList(),
        Tools = r.Tools.Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug }).ToList(),
    };
}
