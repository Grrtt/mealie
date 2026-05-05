using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Queries.Organizers;

public record GetToolBySlugQuery(Guid GroupId, string Slug) : IQuery<ToolResponse?>
{
    public async Task<ToolResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetToolBySlugAsync(services.Db, GroupId, Slug, ct);
    }
}
