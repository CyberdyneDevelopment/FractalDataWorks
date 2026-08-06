using System;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents a summary of analytics metrics for a given time period.
/// </summary>
// Why: pure data-transfer POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class AnalyticsSummary
{
    /// <summary>
    /// Gets or sets the total number of executions.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the average execution duration in milliseconds.
    /// </summary>
    public double AverageDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile execution duration in milliseconds.
    /// </summary>
    public double P95DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate as a ratio between 0 and 1.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the number of unique calculation types executed.
    /// </summary>
    public int UniqueCalculationTypes { get; set; }

    /// <summary>
    /// Gets or sets the number of unique users who triggered executions.
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Gets or sets the start of the analytics period.
    /// </summary>
    public DateTimeOffset PeriodStart { get; set; }

    /// <summary>
    /// Gets or sets the end of the analytics period.
    /// </summary>
    public DateTimeOffset PeriodEnd { get; set; }
}
