namespace Mealie.Application.Contracts.Search;

public interface ISearchIndex<TQuery, TResult>
{
    Task IndexAsync(Guid id, CancellationToken ct = default);
    Task RemoveAsync(Guid id, CancellationToken ct = default);
    Task<TResult> SearchAsync(TQuery query, CancellationToken ct = default);
    Task RebuildAsync(CancellationToken ct = default);
}
