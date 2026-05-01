using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Mealie.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

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

file static class CookbookMappings
{
    public static Mealie.Application.Dtos.Organizers.CookbookResponse MapToResponse(Mealie.Domain.Entities.Organizers.Cookbook c) =>
        new()
        {
            Id = c.Id, Name = c.Name, Description = c.Description, Image = c.Image,
            Public = c.Public, RequireAllCategories = c.RequireAllCategories, Position = c.Position,
            GroupId = c.GroupId, HouseholdId = c.HouseholdId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt
        };
}