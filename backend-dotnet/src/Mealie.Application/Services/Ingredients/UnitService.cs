using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Ingredients;

public class UnitService(ApplicationDbContext db) : IUnitService
{
    public async Task<PaginatedResponse<UnitResponse>> GetUnitsAsync(Guid groupId, PaginationParams pagination,
        string? search = null, CancellationToken ct = default)
    {
        var query = IngredientCrudCore.WithUnitDetails(db.Units.IgnoreQueryFilters()).Where(u => u.GroupId == groupId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Name.Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .ToListAsync(ct);

        return new PaginatedResponse<UnitResponse>
        {
            Page = pagination.Page, PerPage = pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items.Select(IngredientCrudCore.MapToResponse).ToList()
        };
    }

    public async Task<UnitResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var u = await IngredientCrudCore.WithUnitDetails(db.Units.IgnoreQueryFilters())
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == id, ct);
        return u is null ? null : IngredientCrudCore.MapToResponse(u);
    }

    public async Task<UnitResponse> CreateAsync(Guid groupId, CreateUnitRequest request, CancellationToken ct = default)
    {
        var unit = new IngredientUnit
        {
            Id = Guid.NewGuid(), Name = request.Name, Description = request.Description,
            Abbreviation = request.Abbreviation, PluralName = request.PluralName,
            PluralAbbreviation = request.PluralAbbreviation,
            UseAbbreviation = request.UseAbbreviation, Fraction = request.Fraction,
            GroupId = groupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };

        IngredientCrudCore.ReplaceAliases(unit, request.Aliases);

        db.Units.Add(unit);
        await db.SaveChangesAsync(ct);
        return IngredientCrudCore.MapToResponse(unit);
    }

    public async Task<UnitResponse?> UpdateAsync(Guid groupId, Guid id, UpdateUnitRequest request,
        CancellationToken ct = default)
    {
        var unit = await IngredientCrudCore.WithUnitDetails(db.Units.IgnoreQueryFilters())
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == id, ct);
        if (unit is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            unit.Name = request.Name;
        }

        if (request.Description is not null)
        {
            unit.Description = request.Description;
        }

        if (request.Abbreviation is not null)
        {
            unit.Abbreviation = request.Abbreviation;
        }

        if (request.PluralName is not null)
        {
            unit.PluralName = request.PluralName;
        }

        if (request.PluralAbbreviation is not null)
        {
            unit.PluralAbbreviation = request.PluralAbbreviation;
        }

        if (request.UseAbbreviation.HasValue)
        {
            unit.UseAbbreviation = request.UseAbbreviation.Value;
        }

        if (request.Fraction.HasValue)
        {
            unit.Fraction = request.Fraction.Value;
        }

        if (request.Aliases is not null)
        {
            IngredientCrudCore.ReplaceAliases(unit, request.Aliases);
        }

        unit.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return IngredientCrudCore.MapToResponse(unit);
    }

    public async Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        return await IngredientCrudCore.DeleteUnitAsync(db, groupId, id, ct);
    }

    public async Task<bool> MergeAsync(Guid groupId, Guid fromUnitId, Guid toUnitId, CancellationToken ct = default)
    {
        return await IngredientCrudCore.MergeUnitAsync(db, groupId, fromUnitId, toUnitId, ct);
    }

}
