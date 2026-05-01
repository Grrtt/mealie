using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Users;

public record RemoveFavoriteRecipeCommand(Guid UserId, string Slug) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        var recipe = user?.FavoriteRecipes.FirstOrDefault(r => r.Slug == Slug);
        if (recipe is null)
        {
            return false;
        }

        user!.FavoriteRecipes.Remove(recipe);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
