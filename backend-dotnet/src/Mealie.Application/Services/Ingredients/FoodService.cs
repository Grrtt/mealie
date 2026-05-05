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
        var query = IngredientCrudCore.WithFoodDetails(db.Foods.IgnoreQueryFilters()).Where(f => f.GroupId == groupId);

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
            Items = items.Select(IngredientCrudCore.MapToResponse).ToList()
        };
    }

    public async Task<FoodResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var f = await IngredientCrudCore.WithFoodDetails(db.Foods.IgnoreQueryFilters())
            .FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        return f is null ? null : IngredientCrudCore.MapToResponse(f);
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

        IngredientCrudCore.ReplaceAliases(food, request.Aliases);

        db.Foods.Add(food);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new FoodCreatedEvent(food.Id, groupId), ct);

        // Reload with nav properties for correct response
        await db.Entry(food).Reference(f => f.Label).LoadAsync(ct);
        return IngredientCrudCore.MapToResponse(food);
    }

    public async Task<FoodResponse?> UpdateAsync(Guid groupId, Guid id, UpdateFoodRequest request,
        CancellationToken ct = default)
    {
        var food = await IngredientCrudCore.WithFoodDetails(db.Foods.IgnoreQueryFilters())
            .FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        if (food is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            food.Name = request.Name;
        }

        if (request.Description is not null)
        {
            food.Description = request.Description;
        }

        if (request.PluralName is not null)
        {
            food.PluralName = request.PluralName;
        }

        if (request.UnitId.HasValue)
        {
            food.UnitId = request.UnitId;
        }

        if (request.LabelId.HasValue)
        {
            food.LabelId = request.LabelId;
        }

        if (request.OnHand.HasValue)
        {
            food.OnHand = request.OnHand.Value;
        }

        if (request.Aliases is not null)
        {
            IngredientCrudCore.ReplaceAliases(food, request.Aliases);
        }

        food.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new FoodUpdatedEvent(food.Id, groupId), ct);

        if (request.LabelId.HasValue)
        {
            await db.Entry(food).Reference(f => f.Label).LoadAsync(ct);
        }

        return IngredientCrudCore.MapToResponse(food);
    }

    public async Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        return await IngredientCrudCore.DeleteFoodAsync(db, mediator, groupId, id, ct);
    }

    public async Task<bool> MergeAsync(Guid groupId, Guid fromFoodId, Guid toFoodId, CancellationToken ct = default)
    {
        return await IngredientCrudCore.MergeFoodAsync(db, mediator, groupId, fromFoodId, toFoodId, ct);
    }

}
