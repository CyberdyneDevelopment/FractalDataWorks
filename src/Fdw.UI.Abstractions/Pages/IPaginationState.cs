namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Pagination state for list pages.
/// </summary>
public interface IPaginationState
{
    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    int CurrentPage { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of items.
    /// </summary>
    int TotalItems { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    int TotalPages { get; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    bool HasPreviousPage { get; }

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    bool HasNextPage { get; }
}