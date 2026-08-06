using System;
using Fdw.Collections;

namespace Fdw.Services.Resiliency.Abstractions;

/// <summary>
/// Base class for resiliency policy implementations.
/// Provides the common structure for all resiliency policies including
/// retry, backoff, and circuit breaker configuration.
/// </summary>
public abstract class ResiliencyPolicyBase : TypeOptionBase<int, IResiliencyPolicy>, ITypeOption<int, ResiliencyPolicyBase>, IResiliencyPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencyPolicyBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this policy.</param>
    /// <param name="name">Name of the policy.</param>
    protected ResiliencyPolicyBase(int id, string name) : base(id, name)
    {
    }

    /// <summary>
    /// Gets the maximum number of retry attempts before giving up.
    /// </summary>
    public abstract int MaxRetries { get; }

    /// <summary>
    /// Gets the initial delay before the first retry attempt.
    /// </summary>
    public abstract TimeSpan InitialDelay { get; }

    /// <summary>
    /// Gets the maximum delay between retry attempts.
    /// The actual delay will not exceed this value regardless of backoff calculations.
    /// </summary>
    public abstract TimeSpan MaxDelay { get; }

    /// <summary>
    /// Gets the multiplier applied to the delay after each retry attempt.
    /// For example, a value of 2.0 doubles the delay with each attempt.
    /// </summary>
    public abstract double BackoffMultiplier { get; }

    /// <summary>
    /// Gets the duration the circuit breaker remains open after being tripped.
    /// During this time, all operations will fail immediately without attempting execution.
    /// </summary>
    public abstract TimeSpan CircuitBreakerDuration { get; }

    /// <summary>
    /// Gets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    public abstract int CircuitBreakerThreshold { get; }

    /// <summary>
    /// Gets the category of operations this policy is designed for.
    /// </summary>
    public abstract IResiliencyCategory ResiliencyCategory { get; }
}
