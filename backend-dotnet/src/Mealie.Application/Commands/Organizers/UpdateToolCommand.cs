using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Commands.Organizers;

public record UpdateToolCommand(Guid GroupId, Guid Id, UpdateToolRequest Request) : IQuery<ToolResponse?>
{
    public async Task<ToolResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.UpdateToolAsync(services.Db, GroupId, Id, Request, ct);
    }
}
