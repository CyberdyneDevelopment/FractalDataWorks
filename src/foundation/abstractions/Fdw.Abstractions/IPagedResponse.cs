using System.Collections.Generic;

namespace Fdw.Abstractions;

/// <summary>
/// Standard paginated response with items and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public interface IPagedResponse<out T>
{
    /// <summary>
    /// Gets the data items for this page.
    /// </summary>
    IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    int Page { get; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    int PageSize { get; }

    /// <summary>
    /// Gets the total count of items across all pages.
    /// </summary>
    long TotalCount { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    int TotalPages { get; }

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    bool HasPrevious { get; }

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    bool HasNext { get; }
}
