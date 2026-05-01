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