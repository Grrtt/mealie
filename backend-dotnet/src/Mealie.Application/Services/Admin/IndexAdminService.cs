using Mealie.Application.Contracts.Search;
using Mealie.Application.Dtos.Admin;
using Mealie.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Mealie.Application.Services.Admin;

public class IndexAdminService : IIndexAdminService
{
    private readonly IReadOnlyList<IIndexDiagnostics> _indexes;
    private readonly string _dataDir;

    public IndexAdminService(IEnumerable<IIndexDiagnostics> indexes, IOptions<AppSettings> appSettings)
    {
        _indexes = indexes.ToList();
        _dataDir = appSettings.Value.DataDir;
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Sum(f => new FileInfo(f).Length);
    }

    private IndexInfoResponse BuildInfo(IIndexDiagnostics index)
    {
        var dirPath = Path.Combine(_dataDir, "search", index.Name);
        return new IndexInfoResponse(
            index.Name,
            index.GetDocumentCount(),
            dirPath,
            GetDirectorySize(dirPath));
    }

    public IReadOnlyList<IndexInfoResponse> GetAll()
        => _indexes.Select(BuildInfo).ToList();

    public IndexInfoResponse? GetInfo(string name)
    {
        var index = _indexes.FirstOrDefault(i => i.Name == name);
        return index is null ? null : BuildInfo(index);
    }

    public async Task<bool> Rebuild(string name, CancellationToken ct)
    {
        var index = _indexes.FirstOrDefault(i => i.Name == name);
        if (index is null) return false;
        await index.RebuildAsync(ct);
        return true;
    }

    public async Task<bool> Delete(string name, CancellationToken ct)
    {
        var index = _indexes.FirstOrDefault(i => i.Name == name);
        if (index is null) return false;
        await index.DeleteAsync(ct);
        return true;
    }

    public IndexSearchResponse? Search(string name, string? query, int maxResults, CancellationToken ct)
    {
        var index = _indexes.FirstOrDefault(i => i.Name == name);
        if (index is null) return null;
        var docs = index.RawSearch(query, maxResults);
        return new IndexSearchResponse(name, docs.Count, docs);
    }
}
