using Mealie.Application.Contracts.Search;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Recipes;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;
using NutritionDto = Mealie.Application.Dtos.Recipes.NutritionDto;

namespace Mealie.Application.Queries.Recipes;

public record GetPaginatedRecipesQuery(Guid HouseholdId, PaginationParams Pagination, RecipeFilter? Filter = null)
    : IQuery<PaginatedResponse<RecipeSummaryResponse>>
{
    public async Task<PaginatedResponse<RecipeSummaryResponse>> ExecuteAsync(IQueryServices services,
        CancellationToken ct = default)
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
        {
            query = query.Where(r =>
                r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));
        }

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
                : query.Where(r =>
                    r.RecipeIngredients.Any(i => i.FoodId.HasValue && foodGuids.Contains(i.FoodId.Value)));
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
            "name" => Filter?.OrderDirection?.ToLowerInvariant() == "asc"
                ? query.OrderBy(r => r.Name)
                : query.OrderByDescending(r => r.Name),
            "created_at" => Filter?.OrderDirection?.ToLowerInvariant() == "asc"
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt),
            "updated_at" => Filter?.OrderDirection?.ToLowerInvariant() == "asc"
                ? query.OrderBy(r => r.UpdateAt)
                : query.OrderByDescending(r => r.UpdateAt),
            "last_made" => Filter?.OrderDirection?.ToLowerInvariant() == "asc"
                ? query.OrderBy(r => r.LastMade)
                : query.OrderByDescending(r => r.LastMade),
            "rating" => Filter?.OrderDirection?.ToLowerInvariant() == "asc"
                ? query.OrderBy(r => r.Rating)
                : query.OrderByDescending(r => r.Rating),
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
