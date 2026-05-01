using Mealie.Application.Dtos.Organizers;
using Mealie.Domain.Entities.Organizers;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

using Mealie.Application.Queries;
namespace Mealie.Application.Commands.Cookbooks;

public record CreateCookbookCommand(Guid GroupId, Guid HouseholdId, CreateCookbookRequest Request) : IQuery<CookbookResponse>
{
    public async Task<CookbookResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var position = await db.Cookbooks.IgnoreQueryFilters().Where(c => c.HouseholdId == HouseholdId)
            .MaxAsync(c => (int?)c.Position, ct) ?? -1;
        var cookbook = new Cookbook
        {
            Id = Guid.NewGuid(), Name = Request.Name, Description = Request.Description,
            Public = Request.Public, RequireAllCategories = Request.RequireAllCategories,
            Position = position + 1, GroupId = GroupId, HouseholdId = HouseholdId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.Cookbooks.Add(cookbook);
        await db.SaveChangesAsync(ct);
        return CookbookMappings.MapToResponse(cookbook);
    }
}

public record UpdateCookbookCommand(Guid HouseholdId, Guid Id, UpdateCookbookRequest Request) : IQuery<CookbookResponse?>
{
    public async Task<CookbookResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var cookbook = await db.Cookbooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.HouseholdId == HouseholdId && c.Id == Id, ct);
        if (cookbook is null) return null;
        if (Request.Name is not null) cookbook.Name = Request.Name;
        if (Request.Description is not null) cookbook.Description = Request.Description;
        if (Request.Public.HasValue) cookbook.Public = Request.Public.Value;
        if (Request.RequireAllCategories.HasValue) cookbook.RequireAllCategories = Request.RequireAllCategories.Value;
        cookbook.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return CookbookMappings.MapToResponse(cookbook);
    }
}

public record DeleteCookbookCommand(Guid HouseholdId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var cookbook = await db.Cookbooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.HouseholdId == HouseholdId && c.Id == Id, ct);
        if (cookbook is null) return false;
        db.Cookbooks.Remove(cookbook);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public record ReorderCookbooksCommand(Guid HouseholdId, IEnumerable<CookbookReorderRequest> ReorderRequests) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var requests = ReorderRequests.ToList();
        foreach (var req in requests)
        {
            var cookbook = await db.Cookbooks.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.HouseholdId == HouseholdId && c.Id == req.Id, ct);
            if (cookbook is null) return false;
            cookbook.Position = req.Position;
        }
        await db.SaveChangesAsync(ct);
        return true;
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
