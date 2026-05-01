namespace Mealie.Application.Queries;

/// <summary>
///     Represents a self-executing query command. The constructor carries the input
///     parameters; <see cref="ExecuteAsync" /> performs the data access using the
///     common services provided by <see cref="QueryExecutor" />.
/// </summary>
public interface IQuery<TResult>
{
    Task<TResult> ExecuteAsync(IQueryServices services, CancellationToken ct = default);
}
