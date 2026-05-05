using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Commands.Organizers;

public record CreateCategoryCommand(Guid GroupId, CreateOrganizerRequest Request) : IQuery<CategoryResponse>
{
    public async Task<CategoryResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.CreateCategoryAsync(services.Db, GroupId, Request, ct);
    }
}
