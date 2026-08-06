using System.Collections.Generic;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>
/// Paginated response containing schedule summaries and metadata.
/// </summary>
public class SchedulePaginatedResponse
{
    /// <summary>Gets or sets the schedule items for the current page.</summary>
    public IReadOnlyList<ScheduleSummaryDto> Items { get; set; } = [];

    /// <summary>Gets or sets the total number of schedules matching the filter.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets or sets the total number of pages.</summary>
    public int TotalPages { get; set; }
}
