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