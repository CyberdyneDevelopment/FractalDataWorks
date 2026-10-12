using System.ComponentModel.DataAnnotations;

namespace Fdw.Web.RestEndpoints.Pagination;

/// <summary>
/// Base class for paginated requests.
/// Provides standard pagination parameters with validation.
/// </summary>
public class PagedRequest
{
    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "PageSize must be between 1 and 1000")]
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the sort field name.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort direction (asc/desc).
    /// </summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Gets or sets a search/filter term.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Gets the offset for database queries (0-based).
    /// </summary>
    public int Offset => (Page - 1) * PageSize;

    /// <summary>
    /// Gets a value indicating whether the sort direction is descending.
    /// </summary>
    public bool IsDescending => string.Equals(SortDirection?.ToLowerInvariant(), "desc", StringComparison.Ordinal);
}