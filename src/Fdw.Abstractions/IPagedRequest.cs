namespace Fdw.Abstractions;

/// <summary>
/// Standard pagination request parameters.
/// </summary>
public interface IPagedRequest
{
    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    int Page { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    int PageSize { get; }

    /// <summary>
    /// Gets the optional sort field name.
    /// </summary>
    string? SortBy { get; }

    /// <summary>
    /// Gets the sort direction (asc/desc).
    /// </summary>
    string? SortDirection { get; }

    /// <summary>
    /// Gets the optional search/filter term.
    /// </summary>
    string? Search { get; }
}
