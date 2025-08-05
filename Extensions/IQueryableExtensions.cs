using Microsoft.EntityFrameworkCore;
using Todo_List_API.Pagination;

namespace Todo_List_API.Extensions;

public static class QueryableExtensions
{
    public static async Task<PaginatedResult<T>> ToPaginatedListAsync<T>(this IQueryable<T> source, int pageNumber,
        int pageSize)
        where T : class
    {
        if (source == null)
            throw new ArgumentException("Source collection cannot be null.");

        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);

        int count = await source.AsNoTracking().CountAsync();

        if (count == 0)
            return PaginatedResult<T>.Success(new List<T>(), count, pageNumber, pageSize);

        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return PaginatedResult<T>.Success(items, count, pageNumber, pageSize);
    }
}