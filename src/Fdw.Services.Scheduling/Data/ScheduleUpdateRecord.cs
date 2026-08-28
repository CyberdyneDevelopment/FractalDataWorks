using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Scheduling.Data;

/// <summary>
/// POCO record type for updating schedule execution times in the database.
/// Used with DataGateway for schedule update operations.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class ScheduleUpdateRecord
{
    /// <summary>
    /// Gets or sets the last execution time.
    /// </summary>
    public DateTimeOffset? LastRunTime { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled execution time.
    /// </summary>
    public DateTimeOffset? NextRunTime { get; set; }

    /// <summary>
    /// Gets or sets the outcome of the most recent dispatch attempt (e.g. "Succeeded"/"Failed",
    /// the canonical <c>Fdw.Results.ExecutionStatus.ExecutionStatuses</c> names).
    /// </summary>
    public string? LastRunStatus { get; set; }
}
