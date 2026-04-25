using Mealie.Application.Dtos.Organizers;
using Mealie.Domain.Entities.Organizers;
using Mealie.Infrastructure.Data;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Services.Cookbooks;

public class CookbookService(ApplicationDbContext db) : ICookbookService
{
    public async Task<PaginatedResponse<CookbookResponse>> GetCookbooksAsync(Guid householdId, PaginationParams pagination, CancellationToken ct = default)
    {
        var query = db.Cookbooks.IgnoreQueryFilters().Where(c => c.HouseholdId == householdId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Position).ThenBy(c => c.Name)
            .Skip(pagination.Skip).Take(pagination.PerPage)
            .Select(c => MapToResponse(c))
            .ToListAsync(ct);
        return new PaginatedResponse<CookbookResponse> { Page = pagination.Page, PerPage = pagination.PerPage, Total = total, TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage), Items = items };
    }

    public async Task<CookbookResponse?> GetByIdAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var c = await db.Cookbooks.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.HouseholdId == householdId && c.Id == id, ct);
        if (c is null) return null;
        return MapToResponse(c);
    }

    public async Task<CookbookResponse> CreateAsync(Guid groupId, Guid householdId, CreateCookbookRequest request, CancellationToken ct = default)
    {
        var position = await db.Cookbooks.IgnoreQueryFilters().Where(c => c.HouseholdId == householdId).MaxAsync(c => (int?)c.Position, ct) ?? -1;
        var cookbook = new Cookbook
        {
            Id = Guid.NewGuid(), Name = request.Name, Description = request.Description,
            Public = request.Public, RequireAllCategories = request.RequireAllCategories,
            Position = position + 1, GroupId = groupId, HouseholdId = householdId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow,
        };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync(ct);
        return MapToResponse(cookbook);
    }

    public async Task<CookbookResponse?> UpdateAsync(Guid householdId, Guid id, UpdateCookbookRequest request, CancellationToken ct = default)
    {
        var cookbook = await db.Cookbooks.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.HouseholdId == householdId && c.Id == id, ct);
        if (cookbook is null) return null;
        if (request.Name is not null) cookbook.Name = request.Name;
        if (request.Description is not null) cookbook.Description = request.Description;
        if (request.Public.HasValue) cookbook.Public = request.Public.Value;
        if (request.RequireAllCategories.HasValue) cookbook.RequireAllCategories = request.RequireAllCategories.Value;
        cookbook.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToResponse(cookbook);
    }

    public async Task<bool> DeleteAsync(Guid householdId, Guid id, CancellationToken ct = default)
    {
        var cookbook = await db.Cookbooks.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.HouseholdId == householdId && c.Id == id, ct);
        if (cookbook is null) return false;
        db.Cookbooks.Remove(cookbook);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static CookbookResponse MapToResponse(Cookbook c) => new()
    {
        Id = c.Id, Name = c.Name, Description = c.Description, Image = c.Image,
        Public = c.Public, RequireAllCategories = c.RequireAllCategories, Position = c.Position,
        GroupId = c.GroupId, HouseholdId = c.HouseholdId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt,
    };
}
