using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// Generic paginated response wrapper.
/// Contains data and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the data items for this page.
    /// </summary>
    public IEnumerable<T> Data { get; set; } = [];

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total count of items across all pages.
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Gets the count of items in the current page.
    /// </summary>
    public int Count => Data?.Count() ?? 0;

    /// <summary>
    /// Creates a paginated response from a query and total count.
    /// </summary>
    public static PagedResponse<T> Create(IEnumerable<T> data, int page, int pageSize, long totalCount)
    {
        return new PagedResponse<T>
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Creates a paginated response from a paged request.
    /// </summary>
    public static PagedResponse<T> Create(IEnumerable<T> data, PagedRequest request, long totalCount)
    {
        return Create(data, request.Page, request.PageSize, totalCount);
    }
}