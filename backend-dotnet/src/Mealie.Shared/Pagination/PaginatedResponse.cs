namespace Mealie.Shared.Pagination;

public class PaginatedResponse<T>
{
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public IReadOnlyList<T> Items { get; set; } = [];
}
