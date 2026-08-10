using System;

namespace Fdw.Web.Search.Endpoints;

/// <summary>
/// Internal search record for schedules.
/// </summary>
public class SearchableScheduleRecord
{
    /// <summary>Gets or sets the schedule identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the schedule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the associated pipeline name.</summary>
    public string? PipelineName { get; set; }

    /// <summary>Gets or sets the scheduler type.</summary>
    public string? SchedulerType { get; set; }
}