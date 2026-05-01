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
