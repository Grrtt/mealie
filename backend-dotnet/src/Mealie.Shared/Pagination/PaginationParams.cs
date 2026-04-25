namespace Mealie.Shared.Pagination;

public class PaginationParams
{
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 50;

    public int Skip => (Page - 1) * PerPage;
}
