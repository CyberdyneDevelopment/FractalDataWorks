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
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

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
    /// Gets or sets whether to enable circuit breaker.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; }

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// </summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the minimum throughput before circuit breaker activates.
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Gets or sets how long the circuit stays open before testing again.
    /// </summary>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the predicate to determine if an exception should trigger retry.
    /// </summary>
    public Func<Exception, bool>? ShouldRetryOnException { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked on each retry.
    /// </summary>
    public Action<Exception, int, TimeSpan>? OnRetry { get; set; }

    /// <summary>
    /// Creates default resilience options.
    /// </summary>
    public static ResilienceOptions Default => new();

    /// <summary>
    /// Creates resilience options with no retries.
    /// </summary>
    public static ResilienceOptions NoRetry => new() { MaxRetryAttempts = 0 };

    /// <summary>
    /// Creates resilience options with exponential backoff.
    /// </summary>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    /// <param name="initialDelay">Initial delay before first retry.</param>
    public static ResilienceOptions ExponentialBackoff(int maxAttempts = 3, TimeSpan? initialDelay = null) =>
        new()
        {
            MaxRetryAttempts = maxAttempts,
            // BackoffStrategy will be set by the factory based on BackoffStrategies.Exponential
        };
}