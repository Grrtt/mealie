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
        var query = db.Units.IgnoreQueryFilters()
            .Include(u => u.Aliases)
            .Where(u => u.GroupId == groupId);

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
            Items = items.Select(MapToResponse).ToList()
        };
    }

    public async Task<UnitResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var u = await db.Units.IgnoreQueryFilters()
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == id, ct);
        return u is null ? null : MapToResponse(u);
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

        foreach (var alias in request.Aliases)
        {
            unit.Aliases.Add(new IngredientUnitAlias { Id = Guid.NewGuid(), Name = alias, UnitId = unit.Id });
        }

        db.Units.Add(unit);
        await db.SaveChangesAsync(ct);
        return MapToResponse(unit);
    }

    public async Task<UnitResponse?> UpdateAsync(Guid groupId, Guid id, UpdateUnitRequest request,
        CancellationToken ct = default)
    {
        var unit = await db.Units.IgnoreQueryFilters()
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == id, ct);
        if (unit is null) return null;

        if (request.Name is not null) unit.Name = request.Name;
        if (request.Description is not null) unit.Description = request.Description;
        if (request.Abbreviation is not null) unit.Abbreviation = request.Abbreviation;
        if (request.PluralName is not null) unit.PluralName = request.PluralName;
        if (request.PluralAbbreviation is not null) unit.PluralAbbreviation = request.PluralAbbreviation;
        if (request.UseAbbreviation.HasValue) unit.UseAbbreviation = request.UseAbbreviation.Value;
        if (request.Fraction.HasValue) unit.Fraction = request.Fraction.Value;

        if (request.Aliases is not null)
        {
            unit.Aliases.Clear();
            foreach (var alias in request.Aliases)
            {
                unit.Aliases.Add(new IngredientUnitAlias { Id = Guid.NewGuid(), Name = alias, UnitId = unit.Id });
            }
        }

        unit.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(unit);
    }

    public async Task<bool> DeleteAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var unit = await db.Units.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == id, ct);
        if (unit is null) return false;

        db.Units.Remove(unit);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MergeAsync(Guid groupId, Guid fromUnitId, Guid toUnitId, CancellationToken ct = default)
    {
        var fromUnit = await db.Units.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == fromUnitId, ct);
        var toUnit = await db.Units.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == toUnitId, ct);
        if (fromUnit is null || toUnit is null) return false;

        await db.RecipeIngredients
            .Where(i => i.UnitId == fromUnitId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UnitId, toUnitId), ct);

        db.Units.Remove(fromUnit);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static UnitResponse MapToResponse(IngredientUnit u)
    {
        return new UnitResponse
        {
            Id = u.Id, Name = u.Name, Description = u.Description, Abbreviation = u.Abbreviation,
            PluralName = u.PluralName, PluralAbbreviation = u.PluralAbbreviation,
            UseAbbreviation = u.UseAbbreviation, Fraction = u.Fraction,
            GroupId = u.GroupId, CreatedAt = u.CreatedAt, UpdateAt = u.UpdateAt,
            Aliases = u.Aliases.Select(a => new AliasResponse { Id = a.Id, Name = a.Name }).ToList()
        };
    }
}
