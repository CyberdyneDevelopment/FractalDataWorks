namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Request for listing schedules with pagination, sorting, and filtering.
/// </summary>
public class ListSchedulesRequest
{
    /// <summary>Gets or sets the page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the page size (default 25, max 100).</summary>
    public int PageSize { get; set; } = 25;

    /// <summary>Gets or sets the property name to sort by.</summary>
    public string? SortBy { get; set; }

    /// <summary>Gets or sets whether to sort in descending order.</summary>
    public bool SortDescending { get; set; }

    /// <summary>Gets or sets an optional pipeline name filter.</summary>
    public string? PipelineName { get; set; }

    /// <summary>Gets or sets an optional scheduler type filter.</summary>
    public string? SchedulerType { get; set; }

    /// <summary>Gets or sets an optional enabled status filter.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets the validated page number, clamped to a minimum of 1.</summary>
    public int ValidatedPage => Page < 1 ? 1 : Page;

    /// <summary>Gets the validated page size, clamped between 1 and 100.</summary>
    public int ValidatedPageSize => PageSize < 1 ? 25 : (PageSize > 100 ? 100 : PageSize);

    /// <summary>Gets the number of records to skip for the current page.</summary>
    public int Skip => (ValidatedPage - 1) * ValidatedPageSize;
}
