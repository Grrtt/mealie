using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Commands.Organizers;

public record UpdateTagCommand(Guid GroupId, Guid Id, UpdateOrganizerRequest Request) : IQuery<TagResponse?>
{
    public async Task<TagResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.UpdateTagAsync(services.Db, GroupId, Id, Request, ct);
    }
}
