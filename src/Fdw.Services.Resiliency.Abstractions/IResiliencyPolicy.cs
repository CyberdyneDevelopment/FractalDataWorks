using System;
using Fdw.Collections;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Interface defining the contract for resiliency policy options.
/// Resiliency policies define retry, circuit breaker, and backoff behavior
/// for different categories of operations.
/// </summary>
public interface IResiliencyPolicy : ITypeOption<int, IResiliencyPolicy>
{
    /// <summary>
    /// Gets the maximum number of retry attempts before giving up.
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Gets the initial delay before the first retry attempt.
    /// </summary>
    TimeSpan InitialDelay { get; }

    /// <summary>
    /// Gets the maximum delay between retry attempts.
    /// The actual delay will not exceed this value regardless of backoff calculations.
    /// </summary>
    TimeSpan MaxDelay { get; }

    /// <summary>
    /// Gets the multiplier applied to the delay after each retry attempt.
    /// For example, a value of 2.0 doubles the delay with each attempt.
    /// </summary>
    double BackoffMultiplier { get; }

    /// <summary>
    /// Gets the duration the circuit breaker remains open after being tripped.
    /// During this time, all operations will fail immediately without attempting execution.
    /// </summary>
    TimeSpan CircuitBreakerDuration { get; }

    /// <summary>
    /// Gets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    int CircuitBreakerThreshold { get; }

    /// <summary>
    /// Gets the category of operations this policy is designed for.
    /// </summary>
    IResiliencyCategory ResiliencyCategory { get; }
}
