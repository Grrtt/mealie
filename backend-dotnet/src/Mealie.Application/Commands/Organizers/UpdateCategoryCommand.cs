using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Queries;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Commands.Organizers;

public record UpdateCategoryCommand(Guid GroupId, Guid Id, UpdateOrganizerRequest Request) : IQuery<CategoryResponse?>
{
    public async Task<CategoryResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.UpdateCategoryAsync(services.Db, GroupId, Id, Request, ct);
    }
}
