using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Users;

public record AddFavoriteRecipeCommand(Guid UserId, string Slug) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        var recipe = await db.Recipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Slug == Slug, ct);
        if (user is null || recipe is null) return false;
        if (!user.FavoriteRecipes.Any(r => r.Id == recipe.Id))
        {
            user.FavoriteRecipes.Add(recipe);
            await db.SaveChangesAsync(ct);
        }
        return true;
    }
}