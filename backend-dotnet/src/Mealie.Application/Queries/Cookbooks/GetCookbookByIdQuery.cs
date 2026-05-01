using Mealie.Application.Dtos.Organizers;
using Mealie.Domain.Entities.Organizers;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Queries.Cookbooks;

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
