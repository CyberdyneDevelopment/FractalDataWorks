namespace Fdw.Web.Endpoints.Contracts;

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
    /// Gets or sets the page size, or <see langword="null"/> for the <c>DefaultPaginationSize</c> setting.
    /// </summary>
    /// <remarks>
    /// Capped by the <c>MaxPaginationSize</c> setting. Resolve the page with
    /// <c>PaginationQuery.ResolvePage</c>, which refuses a page or page size below 1.
    /// </remarks>
    public int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets the field to sort by.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets whether to sort descending. Default is false (ascending).
    /// </summary>
    public bool SortDescending { get; set; }
}
