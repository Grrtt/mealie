using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Events;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

using Mealie.Application.Queries;
namespace Mealie.Application.Commands.Ingredients;

public record CreateFoodCommand(Guid GroupId, CreateFoodRequest Request) : IQuery<FoodResponse>
{
    public async Task<FoodResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var food = new IngredientFood
        {
            Id = Guid.NewGuid(), Name = Request.Name, Description = Request.Description,
            PluralName = Request.PluralName, UnitId = Request.UnitId, LabelId = Request.LabelId,
            GroupId = GroupId, OnHand = Request.OnHand, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        foreach (var alias in Request.Aliases)
            food.Aliases.Add(new IngredientFoodAlias { Id = Guid.NewGuid(), Name = alias, FoodId = food.Id });

        db.Foods.Add(food);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new FoodCreatedEvent(food.Id, GroupId), ct);
        await db.Entry(food).Reference(f => f.Label).LoadAsync(ct);
        return FoodMappings.MapToResponse(food);
    }
}

public record UpdateFoodCommand(Guid GroupId, Guid Id, UpdateFoodRequest Request) : IQuery<FoodResponse?>
{
    public async Task<FoodResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var food = await db.Foods.IgnoreQueryFilters()
            .Include(f => f.Aliases).Include(f => f.Label)
            .FirstOrDefaultAsync(f => f.GroupId == GroupId && f.Id == Id, ct);
        if (food is null) return null;

        if (Request.Name is not null) food.Name = Request.Name;
        if (Request.Description is not null) food.Description = Request.Description;
        if (Request.PluralName is not null) food.PluralName = Request.PluralName;
        if (Request.UnitId.HasValue) food.UnitId = Request.UnitId;
        if (Request.LabelId.HasValue) food.LabelId = Request.LabelId;
        if (Request.OnHand.HasValue) food.OnHand = Request.OnHand.Value;

        if (Request.Aliases is not null)
        {
            food.Aliases.Clear();
            foreach (var alias in Request.Aliases)
                food.Aliases.Add(new IngredientFoodAlias { Id = Guid.NewGuid(), Name = alias, FoodId = food.Id });
        }

        food.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new FoodUpdatedEvent(food.Id, GroupId), ct);
        if (Request.LabelId.HasValue)
            await db.Entry(food).Reference(f => f.Label).LoadAsync(ct);
        return FoodMappings.MapToResponse(food);
    }
}

public record DeleteFoodCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var food = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == GroupId && f.Id == Id, ct);
        if (food is null) return false;
        db.Foods.Remove(food);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new FoodDeletedEvent(Id), ct);
        return true;
    }
}

public record MergeFoodCommand(Guid GroupId, Guid FromFoodId, Guid ToFoodId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var fromFood = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == GroupId && f.Id == FromFoodId, ct);
        var toFood = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == GroupId && f.Id == ToFoodId, ct);
        if (fromFood is null || toFood is null) return false;

        await db.RecipeIngredients.Where(i => i.FoodId == FromFoodId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.FoodId, ToFoodId), ct);
        db.Foods.Remove(fromFood);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new FoodDeletedEvent(FromFoodId), ct);
        return true;
    }
}

file static class FoodMappings
{
    public static FoodResponse MapToResponse(IngredientFood f) =>
        new()
        {
            Id = f.Id, Name = f.Name, Description = f.Description, PluralName = f.PluralName,
            UnitId = f.UnitId, LabelId = f.LabelId,
            Label = f.Label is null ? null : new LabelSummaryResponse { Id = f.Label.Id, Name = f.Label.Name, Color = f.Label.Color },
            GroupId = f.GroupId, OnHand = f.OnHand,
            Aliases = f.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList(),
            CreatedAt = f.CreatedAt, UpdateAt = f.UpdateAt
        };
}
