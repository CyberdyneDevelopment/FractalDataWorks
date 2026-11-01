namespace Fdw.Web.Clients.Abstractions.Contracts;

using System;

/// <summary>
/// Abstraction for calculation type execution statistics used across Calculations and Analytics domains.
/// </summary>
public interface ICalculationTypeStats
{
    /// <summary>Gets the name of the calculation type.</summary>
    string CalculationType { get; }
    /// <summary>Gets the total number of executions.</summary>
    long ExecutionCount { get; }
    /// <summary>Gets the average execution duration in milliseconds.</summary>
    double AverageDurationMs { get; }
    /// <summary>Gets the minimum execution duration in milliseconds.</summary>
    double MinDurationMs { get; }
    /// <summary>Gets the maximum execution duration in milliseconds.</summary>
    double MaxDurationMs { get; }
    /// <summary>Gets the success rate (0.0 to 100.0).</summary>
    double SuccessRate { get; }
    /// <summary>Gets the cache hit rate (0.0 to 100.0).</summary>
    double CacheHitRate { get; }
    /// <summary>Gets the timestamp of the last execution, or null if never executed.</summary>
    DateTimeOffset? LastExecuted { get; }
}
