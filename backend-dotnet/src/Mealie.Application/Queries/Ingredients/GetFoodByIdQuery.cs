using Mealie.Application.Dtos.Ingredients;
using Mealie.Application.Services.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Ingredients;

public record GetFoodByIdQuery(Guid GroupId, Guid Id) : IQuery<FoodResponse?>
{
    public async Task<FoodResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var f = await IngredientCrudCore.WithFoodDetails(services.Db.Foods.IgnoreQueryFilters())
            .FirstOrDefaultAsync(f => f.GroupId == GroupId && f.Id == Id, ct);
        return f is null ? null : IngredientCrudCore.MapToResponse(f);
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
