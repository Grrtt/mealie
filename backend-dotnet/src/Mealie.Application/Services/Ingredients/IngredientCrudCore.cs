using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Domain.Events;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Ingredients;

public static class IngredientCrudCore
{
    public static IQueryable<IngredientFood> WithFoodDetails(IQueryable<IngredientFood> query)
    {
        return query.Include(f => f.Aliases).Include(f => f.Label);
    }

    public static IQueryable<IngredientUnit> WithUnitDetails(IQueryable<IngredientUnit> query)
    {
        return query.Include(u => u.Aliases);
    }

    public static async Task<PaginatedResponse<FoodResponse>> GetFoodsAsync(
        ApplicationDbContext db,
        Guid groupId,
        PaginationParams pagination,
        string? search,
        CancellationToken ct = default)
    {
        var query = WithFoodDetails(db.Foods.IgnoreQueryFilters()).Where(f => f.GroupId == groupId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f => f.Name.Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(f => f.Name).Skip(pagination.Skip).Take(pagination.PerPage).ToListAsync(ct);
        return new PaginatedResponse<FoodResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Select(MapToResponse).ToList()
        };
    }

    public static async Task<PaginatedResponse<UnitResponse>> GetUnitsAsync(
        ApplicationDbContext db,
        Guid groupId,
        PaginationParams pagination,
        string? search,
        CancellationToken ct = default)
    {
        var query = WithUnitDetails(db.Units.IgnoreQueryFilters()).Where(u => u.GroupId == groupId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Name.Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.Name).Skip(pagination.Skip).Take(pagination.PerPage).ToListAsync(ct);
        return new PaginatedResponse<UnitResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Select(MapToResponse).ToList()
        };
    }

    public static void ReplaceAliases(IngredientFood food, IEnumerable<string> aliases)
    {
        food.Aliases.Clear();
        foreach (var alias in aliases)
        {
            food.Aliases.Add(new IngredientFoodAlias { Id = Guid.NewGuid(), Name = alias, FoodId = food.Id });
        }
    }

    public static void ReplaceAliases(IngredientUnit unit, IEnumerable<string> aliases)
    {
        unit.Aliases.Clear();
        foreach (var alias in aliases)
        {
            unit.Aliases.Add(new IngredientUnitAlias { Id = Guid.NewGuid(), Name = alias, UnitId = unit.Id });
        }
    }

    public static FoodResponse MapToResponse(IngredientFood food)
    {
        return new FoodResponse
        {
            Id = food.Id,
            Name = food.Name,
            Description = food.Description,
            PluralName = food.PluralName,
            UnitId = food.UnitId,
            LabelId = food.LabelId,
            Label = food.Label is null
                ? null
                : new LabelSummaryResponse { Id = food.Label.Id, Name = food.Label.Name, Color = food.Label.Color },
            GroupId = food.GroupId,
            OnHand = food.OnHand,
            Aliases = food.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList(),
            CreatedAt = food.CreatedAt,
            UpdateAt = food.UpdateAt
        };
    }

    public static UnitResponse MapToResponse(IngredientUnit unit)
    {
        return new UnitResponse
        {
            Id = unit.Id,
            Name = unit.Name,
            Description = unit.Description,
            Abbreviation = unit.Abbreviation,
            PluralName = unit.PluralName,
            PluralAbbreviation = unit.PluralAbbreviation,
            UseAbbreviation = unit.UseAbbreviation,
            Fraction = unit.Fraction,
            GroupId = unit.GroupId,
            CreatedAt = unit.CreatedAt,
            UpdateAt = unit.UpdateAt,
            Aliases = unit.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList()
        };
    }

    public static async Task<bool> DeleteFoodAsync(
        ApplicationDbContext db,
        IMediator mediator,
        Guid groupId,
        Guid id,
        CancellationToken ct = default)
    {
        var food = await db.Foods.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == id, ct);
        if (food is null)
        {
            return false;
        }

        db.Foods.Remove(food);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new FoodDeletedEvent(id), ct);
        return true;
    }

    public static async Task<bool> DeleteUnitAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid id,
        CancellationToken ct = default)
    {
        var unit = await db.Units.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == id, ct);
        if (unit is null)
        {
            return false;
        }

        db.Units.Remove(unit);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<bool> MergeFoodAsync(
        ApplicationDbContext db,
        IMediator mediator,
        Guid groupId,
        Guid fromFoodId,
        Guid toFoodId,
        CancellationToken ct = default)
    {
        var fromFood = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == fromFoodId, ct);
        var toFood = await db.Foods.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.GroupId == groupId && f.Id == toFoodId, ct);
        if (fromFood is null || toFood is null)
        {
            return false;
        }

        await db.RecipeIngredients.Where(i => i.FoodId == fromFoodId && i.Recipe.GroupId == groupId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.FoodId, toFoodId), ct);
        db.Foods.Remove(fromFood);
        await db.SaveChangesAsync(ct);
        await mediator.Publish(new FoodDeletedEvent(fromFoodId), ct);
        return true;
    }

    public static async Task<bool> MergeUnitAsync(
        ApplicationDbContext db,
        Guid groupId,
        Guid fromUnitId,
        Guid toUnitId,
        CancellationToken ct = default)
    {
        var fromUnit = await db.Units.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == fromUnitId, ct);
        var toUnit = await db.Units.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == toUnitId, ct);
        if (fromUnit is null || toUnit is null)
        {
            return false;
        }

        await db.RecipeIngredients.Where(i => i.UnitId == fromUnitId && i.Recipe.GroupId == groupId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UnitId, toUnitId), ct);
        db.Units.Remove(fromUnit);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
