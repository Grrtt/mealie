using Mealie.Application.Queries;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record DeleteFoodCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var food = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == GroupId && f.Id == Id, ct);
        if (food is null)
        {
            return false;
        }

        db.Foods.Remove(food);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new FoodDeletedEvent(Id), ct);
        return true;
    }
}
