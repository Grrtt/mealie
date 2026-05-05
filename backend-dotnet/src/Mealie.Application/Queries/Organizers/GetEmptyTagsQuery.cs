using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Queries.Organizers;

public record GetEmptyTagsQuery(Guid GroupId) : IQuery<IList<TagResponse>>
{
    public async Task<IList<TagResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetEmptyTagsAsync(services.Db, GroupId, ct);
    }
}
