using System;
using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents execution statistics for a specific calculation type.
/// </summary>
public sealed class CalculationTypeStats : ICalculationTypeStats
{
    /// <summary>
    /// Gets or sets the calculation type name.
    /// </summary>
    public string CalculationType { get; set; } = "";

    /// <summary>
    /// Gets or sets the total number of executions for this calculation type.
    /// </summary>
    public long ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the average execution duration in milliseconds.
    /// </summary>
    public double AverageDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the minimum execution duration in milliseconds.
    /// </summary>
    public double MinDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum execution duration in milliseconds.
    /// </summary>
    public double MaxDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the success rate as a ratio between 0 and 1.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate as a ratio between 0 and 1.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last execution, or <see langword="null"/> if never executed.
    /// </summary>
    public DateTimeOffset? LastExecuted { get; set; }
}
