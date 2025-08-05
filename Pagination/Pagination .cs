namespace Todo_List_API.Pagination;

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Data { get; private set; }

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalCount { get; private set; }

    public int PageSize { get; private set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    private PaginatedResult(bool succeeded, List<T> data, int count, int page, int pageSize)
    {
        Data = data ?? new List<T>();
        CurrentPage = page;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    }

    public static PaginatedResult<T> Success(List<T> data, int count, int page, int pageSize)
    {
        return new PaginatedResult<T>(true, data, count, page, pageSize);
    }
}