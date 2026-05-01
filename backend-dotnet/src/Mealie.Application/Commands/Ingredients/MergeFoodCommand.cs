using Mealie.Application.Queries;
using Mealie.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

public record MergeFoodCommand(Guid GroupId, Guid FromFoodId, Guid ToFoodId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var fromFood = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.GroupId == GroupId && f.Id == FromFoodId, ct);
        var toFood = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.GroupId == GroupId && f.Id == ToFoodId, ct);
        if (fromFood is null || toFood is null)
        {
            return false;
        }

        await db.RecipeIngredients.Where(i => i.FoodId == FromFoodId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.FoodId, ToFoodId), ct);
        db.Foods.Remove(fromFood);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new FoodDeletedEvent(FromFoodId), ct);
        return true;
    }
}
