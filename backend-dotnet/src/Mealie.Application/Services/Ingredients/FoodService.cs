using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Ingredients;

public class FoodService(ApplicationDbContext db, IMediator mediator) : IFoodService
{
    public async Task<PaginatedResponse<FoodResponse>> GetFoodsAsync(Guid groupId, PaginationParams pagination,
        string? search = null, CancellationToken ct = default)
    {
        var query = db.Foods.IgnoreQueryFilters()
            .Include(f => f.Aliases)
            .Include(f => f.Label)
            .Where(f => f.GroupId == groupId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f => f.Name.Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(f => f.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .ToListAsync(ct);

        return new PaginatedResponse<FoodResponse>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Select(MapToResponse).ToList()
        };
    }

    public async Task<FoodResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var f = await db.Foods.IgnoreQueryFilters()
            .Include(f => f.Aliases)
            .Include(f => f.Label)
            .FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        return f is null ? null : MapToResponse(f);
    }

    public async Task<FoodResponse> CreateAsync(Guid groupId, CreateFoodRequest request, CancellationToken ct = default)
    {
        var food = new IngredientFood
        {
            Id = Guid.NewGuid(), Name = request.Name, Description = request.Description,
            PluralName = request.PluralName, UnitId = request.UnitId, LabelId = request.LabelId,
            GroupId = groupId, OnHand = request.OnHand,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };

        foreach (var alias in request.Aliases)
        {
            food.Aliases.Add(new IngredientFoodAlias { Id = Guid.NewGuid(), Name = alias, FoodId = food.Id });
        }

        db.Foods.Add(food);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new FoodCreatedEvent(food.Id, groupId), ct);

        // Reload with nav properties for correct response
        await db.Entry(food).Reference(f => f.Label).LoadAsync(ct);
        return MapToResponse(food);
    }

    public async Task<FoodResponse?> UpdateAsync(Guid groupId, Guid id, UpdateFoodRequest request,
        CancellationToken ct = default)
    {
        var food = await db.Foods.IgnoreQueryFilters()
            .Include(f => f.Aliases)
            .Include(f => f.Label)
            .FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        if (food is null) return null;

        if (request.Name is not null) food.Name = request.Name;
        if (request.Description is not null) food.Description = request.Description;
        if (request.PluralName is not null) food.PluralName = request.PluralName;
        if (request.UnitId.HasValue) food.UnitId = request.UnitId;
        if (request.LabelId.HasValue) food.LabelId = request.LabelId;
        if (request.OnHand.HasValue) food.OnHand = request.OnHand.Value;

        if (request.Aliases is not null)
        {
            food.Aliases.Clear();
            foreach (var alias in request.Aliases)
            {
                food.Aliases.Add(new IngredientFoodAlias { Id = Guid.NewGuid(), Name = alias, FoodId = food.Id });
            }
        }

        food.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new FoodUpdatedEvent(food.Id, groupId), ct);

        if (request.LabelId.HasValue)
        {
            await db.Entry(food).Reference(f => f.Label).LoadAsync(ct);
        }

        return MapToResponse(food);
    }

    public async Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var food = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        if (food is null) return false;

        db.Foods.Remove(food);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new FoodDeletedEvent(id), ct);
        return true;
    }

    public async Task<bool> MergeAsync(Guid groupId, Guid fromFoodId, Guid toFoodId, CancellationToken ct = default)
    {
        var fromFood = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == fromFoodId, ct);
        var toFood = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == toFoodId, ct);
        if (fromFood is null || toFood is null) return false;

        await db.RecipeIngredients
            .Where(i => i.FoodId == fromFoodId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.FoodId, toFoodId), ct);

        db.Foods.Remove(fromFood);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new FoodDeletedEvent(fromFoodId), ct);
        return true;
    }

    private static FoodResponse MapToResponse(IngredientFood f)
    {
        return new FoodResponse
        {
            Id = f.Id, Name = f.Name, Description = f.Description, PluralName = f.PluralName,
            UnitId = f.UnitId, LabelId = f.LabelId,
            Label = f.Label is null ? null : new LabelSummaryResponse
            {
                Id = f.Label.Id, Name = f.Label.Name, Color = f.Label.Color
            },
            GroupId = f.GroupId, OnHand = f.OnHand,
            Aliases = f.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList(),
            CreatedAt = f.CreatedAt, UpdateAt = f.UpdateAt
        };
    }
}
