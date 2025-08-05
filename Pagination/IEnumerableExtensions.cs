namespace Todo_List_API.Pagination
{
    public static class IEnumerableExtensions
{
    public static PaginatedResult<T> ToPaginatedList<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
        where T : class
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source), "Source collection cannot be null.");

        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        int count = source.Count();

        if (count == 0)
            return PaginatedResult<T>.Success(new List<T>(), count, pageNumber, pageSize);

        var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return PaginatedResult<T>.Success(items, count, pageNumber, pageSize);
    }
}

}