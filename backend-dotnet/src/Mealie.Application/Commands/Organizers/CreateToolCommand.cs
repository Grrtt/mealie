using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Commands.Organizers;

public record CreateToolCommand(Guid GroupId, CreateToolRequest Request) : IQuery<ToolResponse>
{
    public async Task<ToolResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.CreateToolAsync(services.Db, GroupId, Request, ct);
    }
}
