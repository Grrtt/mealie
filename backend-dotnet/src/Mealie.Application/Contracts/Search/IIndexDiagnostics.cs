namespace Mealie.Application.Contracts.Search;

public interface IIndexDiagnostics
{
    string Name { get; }
    int GetDocumentCount();
    IReadOnlyList<IReadOnlyDictionary<string, string>> RawSearch(string? query, int maxResults = 50);
    Task DeleteAsync(CancellationToken ct = default);
    Task RebuildAsync(CancellationToken ct = default);
}
