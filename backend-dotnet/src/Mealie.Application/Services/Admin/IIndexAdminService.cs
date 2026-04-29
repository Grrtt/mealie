using Mealie.Application.Dtos.Admin;

namespace Mealie.Application.Services.Admin;

public interface IIndexAdminService
{
    IReadOnlyList<IndexInfoResponse> GetAll();
    IndexInfoResponse? GetInfo(string name);
    Task<bool> Rebuild(string name, CancellationToken ct);
    Task<bool> Delete(string name, CancellationToken ct);
    IndexSearchResponse? Search(string name, string? query, int maxResults, CancellationToken ct);
}
