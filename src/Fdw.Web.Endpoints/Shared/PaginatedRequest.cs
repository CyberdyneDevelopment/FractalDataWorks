namespace Fdw.Web.Endpoints.Shared;

/// <summary>
/// Base request for paginated endpoints.
/// </summary>
public abstract class PaginatedRequest
{
    /// <summary>
    /// Gets or sets the page number (1-based). Default is 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Default is 25, max is 100.
    /// </summary>
    public int PageSize { get; set; } = 25;

    /// <summary>
    /// Gets or sets the field to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets whether to sort descending. Default is false (ascending).
    /// </summary>
    public bool SortDescending { get; set; }

    /// <summary>
    /// Gets the validated page number (minimum 1).
    /// </summary>
    public int ValidatedPage => Page < 1 ? 1 : Page;

    /// <summary>
    /// Gets the validated page size (between 1 and 100).
    /// </summary>
    public int ValidatedPageSize => PageSize < 1 ? 25 : (PageSize > 100 ? 100 : PageSize);

    /// <summary>
    /// Gets the number of items to skip.
    /// </summary>
    public int Skip => (ValidatedPage - 1) * ValidatedPageSize;
}
