using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Commands.Organizers;

public record CreateTagCommand(Guid GroupId, CreateOrganizerRequest Request) : IQuery<TagResponse>
{
    public async Task<TagResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.CreateTagAsync(services.Db, GroupId, Request, ct);
    }
}
