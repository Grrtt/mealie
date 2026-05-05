using Mealie.Application.Queries;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Commands.Organizers;

public record DeleteToolCommand(Guid GroupId, Guid Id) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.DeleteToolAsync(services.Db, GroupId, Id, ct);
    }
}
