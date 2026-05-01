using Mealie.Application.Common;
using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NutritionDto = Mealie.Application.Dtos.Recipes.NutritionDto;

namespace Mealie.Application.Commands.Recipes;

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