using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Organizers;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Cookbooks;

public record UpdateCookbookCommand(Guid HouseholdId, Guid Id, UpdateCookbookRequest Request)
    : IQuery<CookbookResponse?>
{
    public async Task<CookbookResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var cookbook = await db.Cookbooks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.HouseholdId == HouseholdId && c.Id == Id, ct);
        if (cookbook is null)
        {
            return null;
        }

        if (Request.Name is not null)
        {
            cookbook.Name = Request.Name;
        }

        if (Request.Description is not null)
        {
            cookbook.Description = Request.Description;
        }

        if (Request.Public.HasValue)
        {
            cookbook.Public = Request.Public.Value;
        }

        if (Request.RequireAllCategories.HasValue)
        {
            cookbook.RequireAllCategories = Request.RequireAllCategories.Value;
        }

        cookbook.UpdateAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return CookbookMappings.MapToResponse(cookbook);
    }
}

file static class CookbookMappings
{
    public static CookbookResponse MapToResponse(Cookbook c)
    {
        return new CookbookResponse
        {
            Id = c.Id, Name = c.Name, Description = c.Description, Image = c.Image,
            Public = c.Public, RequireAllCategories = c.RequireAllCategories, Position = c.Position,
            GroupId = c.GroupId, HouseholdId = c.HouseholdId, CreatedAt = c.CreatedAt, UpdateAt = c.UpdateAt
        };
    }
}
