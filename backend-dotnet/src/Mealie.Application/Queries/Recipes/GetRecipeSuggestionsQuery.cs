using Mealie.Application.Dtos.Recipes;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NutritionDto = Mealie.Application.Dtos.Recipes.NutritionDto;

namespace Mealie.Application.Queries.Recipes;

public record GetRecipeSuggestionsQuery(
    Guid HouseholdId,
    Guid GroupId,
    int Limit,
    string? QueryFilter,
    int MaxMissingFoods,
    int MaxMissingTools,
    bool IncludeFoodsOnHand,
    bool IncludeToolsOnHand,
    IList<Guid> FoodIds,
    IList<Guid> ToolIds)
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
            foreach (var id in onHand)
            {
                selectedFoodIds.Add(id);
            }
        }

        if (IncludeToolsOnHand)
        {
            var onHand = await db.Tools.IgnoreQueryFilters()
                .Where(t => t.GroupId == GroupId && t.OnHand).Select(t => t.Id).ToListAsync(ct);
            foreach (var id in onHand)
            {
                selectedToolIds.Add(id);
            }
        }

        var recipes = await db.Recipes.IgnoreQueryFilters()
            .Where(r => r.HouseholdId == HouseholdId)
            .Include(r => r.RecipeIngredients).ThenInclude(i => i.Food)
            .Include(r => r.Tools).Include(r => r.Tags).Include(r => r.Categories).ToListAsync(ct);

        var suggestions = new List<RecipeSuggestionItem>();
        foreach (var recipe in recipes)
        {
            var recipeFoodIds = recipe.RecipeIngredients.Where(i => i.FoodId.HasValue).Select(i => i.FoodId!.Value)
                .Distinct().ToHashSet();
            var recipeToolIds = recipe.Tools.Select(t => t.Id).Distinct().ToHashSet();
            var missingFoods = recipeFoodIds.Where(fid => !selectedFoodIds.Contains(fid)).ToList();
            var missingTools = recipeToolIds.Where(tid => !selectedToolIds.Contains(tid)).ToList();
            if (missingFoods.Count > MaxMissingFoods || missingTools.Count > MaxMissingTools)
            {
                continue;
            }

            if (FoodIds.Count > 0 && !recipeFoodIds.Any(fid => FoodIds.Contains(fid)))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(QueryFilter))
            {
                var q = QueryFilter.ToLowerInvariant();
                if (!recipe.Name.ToLowerInvariant().Contains(q) &&
                    !(recipe.Description?.ToLowerInvariant().Contains(q) ?? false))
                {
                    continue;
                }
            }

            var missingFoodDetails = await db.Foods.IgnoreQueryFilters()
                .Where(f => missingFoods.Contains(f.Id))
                .Select(f => new RecipeIngredientFoodDto
                    { Id = f.Id, Name = f.Name, PluralName = f.PluralName, Description = f.Description })
                .ToListAsync(ct);
            var missingToolDetails = await db.Tools.IgnoreQueryFilters()
                .Where(t => missingTools.Contains(t.Id))
                .Select(t => new OrganizerSimpleResponse { Id = t.Id, Name = t.Name, Slug = t.Slug })
                .ToListAsync(ct);

            suggestions.Add(new RecipeSuggestionItem
            {
                Recipe = RecipeCommandMappings.MapToSummary(recipe), MissingFoods = missingFoodDetails,
                MissingTools = missingToolDetails
            });
        }

        var result = suggestions
            .OrderBy(s => s.MissingTools.Count).ThenBy(s => s.MissingFoods.Count)
            .ThenByDescending(s =>
            {
                var rFoodIds = recipes.First(r => r.Id == s.Recipe.Id)
                    .RecipeIngredients.Where(i => i.IsFood && i.FoodId.HasValue).Select(i => i.FoodId!.Value)
                    .ToHashSet();
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
        {
            candidate = $"{slug}-{counter++}";
        }

        return candidate;
    }

    public static RecipeSummaryResponse MapToSummary(Recipe r)
    {
        return new RecipeSummaryResponse
        {
            Id = r.Id, Name = r.Name, Slug = r.Slug, Description = r.Description,
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
