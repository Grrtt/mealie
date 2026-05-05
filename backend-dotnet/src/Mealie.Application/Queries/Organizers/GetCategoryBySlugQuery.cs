using Mealie.Application.Dtos.Organizers;
using Mealie.Application.Services.Organizers;

namespace Mealie.Application.Queries.Organizers;

public record GetCategoryBySlugQuery(Guid GroupId, string Slug) : IQuery<CategoryResponse?>
{
    public async Task<CategoryResponse?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        return await OrganizerCrudModule.GetCategoryBySlugAsync(services.Db, GroupId, Slug, ct);
    }
}
