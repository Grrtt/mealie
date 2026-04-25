using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Ingredients;

public class FoodService(ApplicationDbContext db) : IFoodService
{
    public async Task<PaginatedResponse<FoodResponse>> GetFoodsAsync(Guid groupId, PaginationParams pagination, CancellationToken ct = default)
    {
        var query = db.Foods.IgnoreQueryFilters().Where(f => f.GroupId == groupId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(f => f.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(f => MapToResponse(f))
            .ToListAsync(ct);
        return new PaginatedResponse<FoodResponse> { Page = pagination.Page, PerPage = pagination.PerPage, Total = total, TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), Items = items };
    }

    public async Task<FoodResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var f = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        if (f is null) return null;
        return MapToResponse(f);
    }

    public async Task<FoodResponse> CreateAsync(Guid groupId, CreateFoodRequest request, CancellationToken ct = default)
    {
        var food = new IngredientFood
        {
            Id = Guid.NewGuid(), Name = request.Name, Description = request.Description,
            PluralName = request.PluralName, UnitId = request.UnitId,
            GroupId = groupId, OnHand = request.OnHand,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow,
        };
        db.Foods.Add(food);
        await db.SaveChangesAsync(ct);
        return MapToResponse(food);
    }

    public async Task<FoodResponse?> UpdateAsync(Guid groupId, Guid id, UpdateFoodRequest request, CancellationToken ct = default)
    {
        var food = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        if (food is null) return null;
        if (request.Name is not null) food.Name = request.Name;
        if (request.Description is not null) food.Description = request.Description;
        if (request.PluralName is not null) food.PluralName = request.PluralName;
        if (request.UnitId.HasValue) food.UnitId = request.UnitId;
        if (request.OnHand.HasValue) food.OnHand = request.OnHand.Value;
        food.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(food);
    }

    public async Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var food = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        if (food is null) return false;
        db.Foods.Remove(food);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static FoodResponse MapToResponse(IngredientFood f) => new()
    {
        Id = f.Id, Name = f.Name, Description = f.Description, PluralName = f.PluralName,
        UnitId = f.UnitId, GroupId = f.GroupId, OnHand = f.OnHand,
        CreatedAt = f.CreatedAt, UpdateAt = f.UpdateAt,
    };
}
