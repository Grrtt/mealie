using Mealie.Application.Queries;

namespace Mealie.Application.Commands.Recipes;

public record DeleteShareTokenCommand(Guid TokenId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var token = await services.Db.RecipeShareTokens.FindAsync([TokenId], ct);
        if (token is null)
        {
            return false;
        }

        services.Db.RecipeShareTokens.Remove(token);
        await services.Db.SaveChangesAsync(ct);
        return true;
    }
}
