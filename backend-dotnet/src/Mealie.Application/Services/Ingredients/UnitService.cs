using Mealie.Application.Dtos.Ingredients;
using Mealie.Domain.Entities.Ingredients;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Ingredients;

public class UnitService(ApplicationDbContext db) : IUnitService
{
    public async Task<PaginatedResponse<UnitResponse>> GetUnitsAsync(Guid groupId, PaginationParams pagination, CancellationToken ct = default)
    {
        var query = db.Units.IgnoreQueryFilters().Where(u => u.GroupId == groupId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(u => MapToResponse(u))
            .ToListAsync(ct);
        return new PaginatedResponse<UnitResponse> { Page = pagination.Page, PerPage = pagination.PerPage, Total = total, TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), Items = items };
    }

    public async Task<UnitResponse?> GetByIdAsync(Guid groupId, Guid id, CancellationToken ct = default)
    {
        var u = await db.Units.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == id, ct);
        if (u is null) return null;
        return MapToResponse(u);
    }

    public async Task<UnitResponse> CreateAsync(Guid groupId, CreateUnitRequest request, CancellationToken ct = default)
    {
        var unit = new IngredientUnit
        {
            Id = Guid.NewGuid(), Name = request.Name, Description = request.Description,
            Abbreviation = request.Abbreviation, PluralName = request.PluralName,
            PluralAbbreviation = request.PluralAbbreviation,
            UseAbbreviation = request.UseAbbreviation, Fraction = request.Fraction,
            GroupId = groupId, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow,
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync(ct);
        return MapToResponse(unit);
    }

    public async Task<UnitResponse?> UpdateAsync(Guid groupId, Guid id, UpdateUnitRequest request, CancellationToken ct = default)
    {
        var unit = await db.Units.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.GroupId == groupId && u.Id == id, ct);
        if (unit is null) return null;
        if (request.Name is not null) unit.Name = request.Name;
        if (request.Description is not null) unit.Description = request.Description;
        if (request.Abbreviation is not null) unit.Abbreviation = request.Abbreviation;
        if (request.PluralName is not null) unit.PluralName = request.PluralName;
        if (request.PluralAbbreviation is not null) unit.PluralAbbreviation = request.PluralAbbreviation;
        if (request.UseAbbreviation.HasValue) unit.UseAbbreviation = request.UseAbbreviation.Value;
        if (request.Fraction.HasValue) unit.Fraction = request.Fraction.Value;
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

    private static UnitResponse MapToResponse(IngredientUnit u) => new()
    {
        Id = u.Id, Name = u.Name, Description = u.Description, Abbreviation = u.Abbreviation,
        PluralName = u.PluralName, PluralAbbreviation = u.PluralAbbreviation,
        UseAbbreviation = u.UseAbbreviation, Fraction = u.Fraction,
        GroupId = u.GroupId, CreatedAt = u.CreatedAt, UpdateAt = u.UpdateAt,
    };
}
