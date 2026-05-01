namespace Mealie.Application.Queries;

/// <summary>
/// Dispatches <see cref="IQuery{TResult}"/> instances by injecting the shared
/// <see cref="IQueryServices"/> and delegating execution to the query itself.
/// Register as <c>Scoped</c> so it shares the request lifetime with the DbContext.
/// </summary>
public class QueryExecutor(IQueryServices services)
{
    public Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
        => query.ExecuteAsync(services, ct);
}
