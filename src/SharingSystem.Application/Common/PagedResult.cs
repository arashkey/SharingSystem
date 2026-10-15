using Microsoft.EntityFrameworkCore;

namespace SharingSystem.Application.Common;

/// <summary>Page size limits used by paged lists.</summary>
public static class Pagination
{
    public const int FirstPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
}

/// <summary>Navigation information of a paged list, independent of the item type.</summary>
public interface IPagedResult
{
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }

    /// <summary>1-based number of the first item on the page, or <c>0</c> when the list is empty.</summary>
    int FirstItemNumber { get; }

    /// <summary>1-based number of the last item on the page, or <c>0</c> when the list is empty.</summary>
    int LastItemNumber { get; }
}

/// <summary>One page of a list.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount) : IPagedResult
{
    public int TotalPages => Math.Max(Pagination.FirstPage, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public int FirstItemNumber => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int LastItemNumber => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + Items.Count;
}

public static class PagedQueryExtensions
{
    /// <summary>
    /// Loads one page of an ordered query. The page size is limited to <see cref="Pagination.MaxPageSize"/>
    /// and pages outside the valid range return the nearest existing page.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(Pagination.FirstPage, (int)Math.Ceiling(totalCount / (double)size));
        var currentPage = Math.Clamp(page, Pagination.FirstPage, totalPages);

        var items = await query
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, currentPage, size, totalCount);
    }
}
