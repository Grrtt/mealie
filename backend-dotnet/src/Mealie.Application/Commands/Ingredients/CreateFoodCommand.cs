using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Events;

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
        {
            food.Aliases.Add(new IngredientFoodAlias { Id = Guid.NewGuid(), Name = alias, FoodId = food.Id });
        }

        db.Foods.Add(food);
        await db.SaveChangesAsync(ct);
        await services.Mediator.Publish(new FoodCreatedEvent(food.Id, GroupId), ct);
        await db.Entry(food).Reference(f => f.Label).LoadAsync(ct);
        return FoodMappings.MapToResponse(food);
    }
}

file static class FoodMappings
{
    public static FoodResponse MapToResponse(IngredientFood f)
    {
        return new FoodResponse
        {
            Id = f.Id, Name = f.Name, Description = f.Description, PluralName = f.PluralName,
            UnitId = f.UnitId, LabelId = f.LabelId,
            Label = f.Label is null
                ? null
                : new LabelSummaryResponse { Id = f.Label.Id, Name = f.Label.Name, Color = f.Label.Color },
            GroupId = f.GroupId, OnHand = f.OnHand,
            Aliases = f.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList(),
            CreatedAt = f.CreatedAt, UpdateAt = f.UpdateAt
        };
    }
}
