public class PaginationFilter
{
    private const int MaxPageSize = 50;
    
    public int PageNumber { get; set; } = 1; // Default to page 1
    private int _pageSize = 10;              // Default to 10 items per page

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1 ? 1 : value);
    }
}