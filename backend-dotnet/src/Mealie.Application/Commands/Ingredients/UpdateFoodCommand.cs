using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Events;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Ingredients;

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