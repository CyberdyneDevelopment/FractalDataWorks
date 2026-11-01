using System;
using System.Collections.Generic;

namespace Fdw.Abstractions;

/// <summary>
/// Default implementation of <see cref="IPagedResponse{T}"/>.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public sealed class PagedResponse<T> : IPagedResponse<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResponse{T}"/> class.
    /// </summary>
    /// <param name="items">The items for this page.</param>
    /// <param name="page">The current page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="totalCount">The total count of items across all pages.</param>
    public PagedResponse(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <inheritdoc />
    public IReadOnlyList<T> Items { get; }

    /// <inheritdoc />
    public int Page { get; }

    /// <inheritdoc />
    public int PageSize { get; }

    /// <inheritdoc />
    public long TotalCount { get; }

    /// <inheritdoc />
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <inheritdoc />
    public bool HasPrevious => Page > 1;

    /// <inheritdoc />
    public bool HasNext => Page < TotalPages;
}
