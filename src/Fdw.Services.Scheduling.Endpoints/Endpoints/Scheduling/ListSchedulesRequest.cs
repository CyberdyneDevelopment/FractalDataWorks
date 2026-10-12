namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Request for listing schedules with pagination, sorting, and filtering.
/// </summary>
public class ListSchedulesRequest
{
    /// <summary>Gets or sets the page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the page size, or <see langword="null"/> for the <c>DefaultPaginationSize</c> setting; capped by <c>MaxPaginationSize</c>.</summary>
    public int? PageSize { get; set; }

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
}
