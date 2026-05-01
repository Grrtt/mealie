using Mealie.Application.Dtos.Organizers;
using Mealie.Domain.Entities.Organizers;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Cookbooks;

public record GetCookbooksQuery(Guid HouseholdId, PaginationParams Pagination) : IQuery<PaginatedResponse<CookbookResponse>>
{
    public async Task<PaginatedResponse<CookbookResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var query = db.Cookbooks.IgnoreQueryFilters().Where(c => c.HouseholdId == HouseholdId);
        var total = await query.CountAsync(ct);
        var rawItems = await query.OrderBy(c => c.Position).ThenBy(c => c.Name)
            .Skip(Pagination.Skip).Take(Pagination.PerPage)
            .ToListAsync(ct);
        var items = rawItems.Select(c => CookbookMappings.MapToResponse(c)).ToList();
        return new PaginatedResponse<CookbookResponse>
        {
            Page = Pagination.Page, PerPage = Pagination.PerPage, Total = total,
            TotalPages = (int)Math.Ceiling((double)total / Pagination.PerPage), Items = items
        };
    }
}

public record GetCookbookByIdQuery(Guid HouseholdId, Guid Id) : IQuery<CookbookResponse?>
{
    public async Task<CookbookResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var c = await services.Db.Cookbooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.HouseholdId == HouseholdId && c.Id == Id, ct);
        return c is null ? null : CookbookMappings.MapToResponse(c);
    }
}

file static class CookbookMappings
{
    public static CookbookResponse MapToResponse(Cookbook c) =>
        new()
        {
            Id = c.Id, Name = c.Name, Description = c.Description, Image = c.Image,
            Public = c.Public, RequireAllCategories = c.RequireAllCategories, Position = c.Position,
            GroupId = c.GroupId, HouseholdId = c.HouseholdId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt
        };
}
