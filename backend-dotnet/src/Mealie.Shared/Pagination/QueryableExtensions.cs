using Microsoft.EntityFrameworkCore;

namespace Mealie.Shared.Pagination;

public static class QueryableExtensions
{
    public static async Task<PaginatedResponse<T>> ToPaginatedAsync<T>(
        this IQueryable<T> query, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<T>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items
        };
    }
}
