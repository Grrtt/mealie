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