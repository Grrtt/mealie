using Mealie.Application.Queries;

namespace Mealie.Application.Commands.Recipes;

public record DeleteTimelineEventCommand(Guid EventId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var ev = await db.RecipeTimelineEvents.FindAsync([EventId], ct);
        if (ev is null)
        {
            return false;
        }

        db.RecipeTimelineEvents.Remove(ev);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
