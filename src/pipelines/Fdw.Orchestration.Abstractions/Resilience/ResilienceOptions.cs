using System;
using Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;
using Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;
using Fdw.Orchestration.Abstractions.TypeCollections;

namespace Fdw.Orchestration.Abstractions.Resilience;

/// <summary>
/// Options for configuring resilience behavior.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts. The builder states it from the step's configuration; nothing assumes one
    /// (FDW-3931: "you are defining a resilience policy. why in the fuck would you have a default?").
    /// </summary>
    public int MaxRetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets the backoff strategy for retries.
    /// </summary>
    public IBackoffStrategy? BackoffStrategy { get; set; }

    /// <summary>
    /// Gets or sets the error handling mode.
    /// </summary>
    public IErrorHandlingMode? ErrorHandlingMode { get; set; }

    /// <summary>
    /// Gets or sets the timeout for the operation.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker, every value of which its definer states; null for none (FDW-3931).
    /// </summary>
    public CircuitBreakerOptions? CircuitBreaker { get; set; }

    /// <summary>
    /// Gets or sets the predicate to determine if an exception should trigger retry.
    /// </summary>
    public Func<Exception, bool>? ShouldRetryOnException { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked on each retry.
    /// </summary>
    public Action<Exception, int, TimeSpan>? OnRetry { get; set; }

    /// <summary>
    /// Creates resilience options with no retries.
    /// </summary>
    public static ResilienceOptions NoRetry => new() { MaxRetryAttempts = 0 };

    /// <summary>
    /// Creates resilience options with exponential backoff.
    /// </summary>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    public static ResilienceOptions ExponentialBackoff(int maxAttempts) =>
        new()
        {
            MaxRetryAttempts = maxAttempts,
            // BackoffStrategy will be set by the factory based on BackoffStrategies.Exponential
        };
}