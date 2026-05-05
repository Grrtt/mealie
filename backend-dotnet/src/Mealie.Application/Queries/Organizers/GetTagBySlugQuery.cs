using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Queries.Organizers;

public record GetTagBySlugQuery(Guid GroupId, string Slug) : IQuery<TagResponse?>
{
    public async Task<TagResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetTagBySlugAsync(services.Db, GroupId, Slug, ct);
    }
}
