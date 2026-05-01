using Mealie.Application.Dtos.Users;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Users;

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
