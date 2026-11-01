using System.Collections.Generic;

namespace Fdw.Web.RestEndpoints.Models;

/// <summary>
/// Response wrapper for paginated list endpoints.
/// Contains the items for the current page and pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public sealed class PaginatedResponse<T>
{
    /// <summary>
    /// Gets the items for this page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// Gets the number of items skipped.
    /// </summary>
    public required int Skip { get; init; }

    /// <summary>
    /// Gets the maximum number of items requested.
    /// </summary>
    public required int Take { get; init; }

    /// <summary>
    /// Gets the total number of items available across all pages.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are more items beyond this page.
    /// </summary>
    public bool HasMore => Skip + Items.Count < TotalCount;

    /// <summary>
    /// Creates a new <see cref="PaginatedResponse{T}"/> from the provided data.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="skip">The number of items skipped.</param>
    /// <param name="take">The maximum number of items requested.</param>
    /// <param name="totalCount">The total number of items available.</param>
    /// <returns>A new paginated response instance.</returns>
    public static PaginatedResponse<T> Create(
        IReadOnlyList<T> items,
        int skip,
        int take,
        int totalCount)
    {
        return new PaginatedResponse<T>
        {
            Items = items,
            Skip = skip,
            Take = take,
            TotalCount = totalCount
        };
    }
}
