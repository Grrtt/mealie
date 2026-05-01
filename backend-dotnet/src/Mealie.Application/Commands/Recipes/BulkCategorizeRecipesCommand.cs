using Mealie.Application.Common;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Recipes;

public record BulkCategorizeRecipesCommand(IList<string> Slugs, IList<string> CategoryNames, Guid GroupId)
    : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var recipes = await db.Recipes.Include(r => r.Categories).Where(r => Slugs.Contains(r.Slug)).ToListAsync(ct);
        foreach (var catName in CategoryNames)
        {
            var catSlug = SlugHelper.Generate(catName);
            var cat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == catSlug && c.GroupId == GroupId, ct)
                      ?? new Category
                      {
                          Id = Guid.NewGuid(), Name = catName, Slug = catSlug, GroupId = GroupId,
                          CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
                      };
            if (cat.Id == Guid.Empty || !db.Categories.Local.Contains(cat))
            {
                db.Categories.Add(cat);
            }

            foreach (var recipe in recipes)
            {
                if (!recipe.Categories.Any(c => c.Slug == catSlug))
                {
                    recipe.Categories.Add(cat);
                }
            }
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}
