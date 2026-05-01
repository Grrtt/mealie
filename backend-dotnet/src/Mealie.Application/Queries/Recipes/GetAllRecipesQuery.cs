using Mealie.Application.Common;
using Mealie.Application.Contracts.Search;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Recipes;
using Mealie.Domain.Entities.Recipes;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;
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
