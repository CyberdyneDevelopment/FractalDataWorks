using System;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.Pipelines.Abstractions;

/// <summary>
/// Defines a retry policy for handling transient failures in ETL pipeline stages.
/// </summary>
public interface IPipelineRetryPolicy
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Gets the initial delay between retries.
    /// </summary>
    TimeSpan InitialDelay { get; }

    /// <summary>
    /// Gets the maximum delay between retries.
    /// </summary>
    TimeSpan MaxDelay { get; }

    /// <summary>
    /// Gets the backoff strategy.
    /// </summary>
    BackoffStrategyBase BackoffStrategy { get; }

    /// <summary>
    /// Gets the jitter to add to delays (prevents thundering herd).
    /// </summary>
    TimeSpan? Jitter { get; }

    /// <summary>
    /// Determines whether an exception should be retried.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the operation should be retried.</returns>
    bool ShouldRetry(Exception exception);

    /// <summary>
    /// Calculates the delay before the next retry attempt.
    /// </summary>
    /// <param name="attemptNumber">The retry attempt number (1-based).</param>
    /// <returns>The delay before the next retry.</returns>
    TimeSpan CalculateDelay(int attemptNumber);
}
