using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Recipes;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NutritionDto = Mealie.Application.Dtos.Recipes.NutritionDto;

namespace Mealie.Application.Commands.Recipes;

public record UpdateRecipeCommand(Guid GroupId, string Slug, UpdateRecipeRequest Request)
    : IQuery<RecipeDetailResponse?>
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
        if (recipe is null)
        {
            return null;
        }

        if (Request.Name is not null)
        {
            recipe.Name = Request.Name;
        }

        if (Request.Description is not null)
        {
            recipe.Description = Request.Description;
        }

        if (Request.RecipeYield is not null)
        {
            recipe.RecipeYield = Request.RecipeYield;
        }

        if (Request.TotalTime is not null)
        {
            recipe.TotalTime = Request.TotalTime;
        }

        if (Request.PrepTime is not null)
        {
            recipe.PrepTime = Request.PrepTime;
        }

        if (Request.CookTime is not null)
        {
            recipe.CookTime = Request.CookTime;
        }

        if (Request.PerformTime is not null)
        {
            recipe.PerformTime = Request.PerformTime;
        }

        if (Request.Rating.HasValue)
        {
            recipe.Rating = Request.Rating;
        }

        if (Request.DisableAmount.HasValue)
        {
            recipe.DisableAmount = Request.DisableAmount.Value;
        }

        if (Request.OrgUrl is not null)
        {
            recipe.OrgUrl = Request.OrgUrl;
        }

        if (Request.LastMade.HasValue)
        {
            recipe.LastMade = Request.LastMade;
        }

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
            {
                recipe.RecipeInstructions.Add(new RecipeInstruction
                {
                    Id = inst.Id ?? Guid.NewGuid(), Position = inst.Position,
                    Text = inst.Text, Title = inst.Title, Summary = inst.Summary, RecipeId = recipe.Id
                });
            }
        }

        if (Request.Notes is not null)
        {
            db.RecipeNotes.RemoveRange(recipe.Notes);
            recipe.Notes.Clear();
            foreach (var note in Request.Notes)
            {
                recipe.Notes.Add(new RecipeNote
                    { Id = note.Id ?? Guid.NewGuid(), Title = note.Title, Text = note.Text, RecipeId = recipe.Id });
            }
        }

        if (Request.Tags is not null)
        {
            recipe.Tags.Clear();
            foreach (var tagSlug in Request.Tags)
            {
                var tag = await db.Tags.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == tagSlug && t.GroupId == GroupId, ct);
                if (tag is not null)
                {
                    recipe.Tags.Add(tag);
                }
            }
        }

        if (Request.Categories is not null)
        {
            recipe.Categories.Clear();
            foreach (var catSlug in Request.Categories)
            {
                var cat = await db.Categories.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == GroupId, ct);
                if (cat is not null)
                {
                    recipe.Categories.Add(cat);
                }
            }
        }

        if (Request.Tools is not null)
        {
            recipe.Tools.Clear();
            foreach (var toolSlug in Request.Tools)
            {
                var tool = await db.Tools.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Slug == toolSlug && t.GroupId == GroupId, ct);
                if (tool is not null)
                {
                    recipe.Tools.Add(tool);
                }
            }
        }

        recipe.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new RecipeUpdatedEvent(recipe.Id, recipe.HouseholdId), ct);

        foreach (var ing in recipe.RecipeIngredients)
        {
            if (ing.UnitId.HasValue && ing.Unit is null)
            {
                await db.Entry(ing).Reference(i => i.Unit).LoadAsync(ct);
            }

            if (ing.FoodId.HasValue && ing.Food is null)
            {
                await db.Entry(ing).Reference(i => i.Food).LoadAsync(ct);
            }
        }

        return RecipeCommandMappings.MapToDetail(recipe);
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
