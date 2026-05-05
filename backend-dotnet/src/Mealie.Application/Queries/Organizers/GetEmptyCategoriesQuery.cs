using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Queries.Organizers;

public record GetEmptyCategoriesQuery(Guid GroupId) : IQuery<IList<CategoryResponse>>
{
    public async Task<IList<CategoryResponse>> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetEmptyCategoriesAsync(services.Db, GroupId, ct);
    }
}
