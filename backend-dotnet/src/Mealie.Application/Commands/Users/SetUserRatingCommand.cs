using Mealie.Application.Queries;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Users;

public record SetUserRatingCommand(Guid UserId, string Slug, int? Rating, bool? IsFavorite) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        if (IsFavorite == true)
        {
            await new AddFavoriteRecipeCommand(UserId, Slug).ExecuteAsync(services, ct);
        }
        else if (IsFavorite == false)
        {
            await new RemoveFavoriteRecipeCommand(UserId, Slug).ExecuteAsync(services, ct);
        }

        if (Rating.HasValue)
        {
            var recipe = await db.Recipes.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Slug == Slug, ct);
            if (recipe is not null)
            {
                recipe.Rating = Rating;
                await db.SaveChangesAsync(ct);
            }
        }

        return true;
    }
}
