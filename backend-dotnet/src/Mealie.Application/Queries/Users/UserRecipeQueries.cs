using Mealie.Application.Dtos.Users;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Users;

public record GetFavoriteRecipesQuery(Guid UserId) : IQuery<IList<string>>
{
    public async Task<IList<string>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var user = await services.Db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        return user is null ? [] : user.FavoriteRecipes.Select(r => r.Slug).ToList();
    }
}

public record GetUserRatingsQuery(Guid UserId) : IQuery<IList<UserRatingResponse>>
{
    public async Task<IList<UserRatingResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var user = await services.Db.Users.IgnoreQueryFilters()
            .Include(u => u.FavoriteRecipes)
            .FirstOrDefaultAsync(u => u.Id == UserId, ct);
        if (user is null) return [];
        return user.FavoriteRecipes.Select(r => new UserRatingResponse
        {
            Id = r.Id, RecipeId = r.Id, Slug = r.Slug, IsFavorite = true, Rating = r.Rating
        }).ToList();
    }
}
