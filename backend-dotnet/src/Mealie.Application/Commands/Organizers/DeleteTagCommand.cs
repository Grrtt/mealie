using Mealie.Application.Queries;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Commands.Organizers;

public record DeleteTagCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.DeleteTagAsync(services.Db, GroupId, Id, ct);
    }
}
