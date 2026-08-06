using System.Collections.Generic;

namespace Fdw.Orchestration.Abstractions;

/// <summary>
/// Metrics collected during orchestration execution.
/// </summary>
public interface IOrchestrationMetrics
{
    /// <summary>
    /// Gets the total number of steps executed.
    /// </summary>
    int TotalSteps { get; }

    /// <summary>
    /// Gets the number of steps that succeeded.
    /// </summary>
    int SucceededSteps { get; }

    /// <summary>
    /// Gets the number of steps that failed.
    /// </summary>
    int FailedSteps { get; }

    /// <summary>
    /// Gets the number of steps that were skipped.
    /// </summary>
    int SkippedSteps { get; }

    /// <summary>
    /// Gets the total number of retry attempts across all steps.
    /// </summary>
    int TotalRetryAttempts { get; }

    /// <summary>
    /// Gets the total records processed across all steps.
    /// </summary>
    long TotalRecordsProcessed { get; }

    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    int CacheHits { get; }

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    int CacheMisses { get; }

    /// <summary>
    /// Gets custom metrics collected during execution.
    /// </summary>
    IReadOnlyDictionary<string, object> CustomMetrics { get; }
}