using System;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions.Policies;

/// <summary>
/// Simple resiliency policy for basic operations.
/// Designed for operations requiring minimal retry logic with short delays
/// for quick failure recovery.
/// </summary>
/// <remarks>
/// <para>
/// This policy uses low retry counts and short delays, making it suitable
/// for operations that should either succeed quickly or fail fast.
/// Examples include local file operations, cache lookups, or in-memory
/// operations that may have transient contention.
/// </para>
/// <para>
/// The circuit breaker is configured with a low threshold to quickly
/// detect and respond to systemic issues.
/// </para>
/// </remarks>
[TypeOption(typeof(ResiliencyPolicies), "Simple", RestrictToCurrentCompilation = true)]
public sealed class SimpleRetryResiliencyPolicy : ResiliencyPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleRetryResiliencyPolicy"/> class.
    /// </summary>
    public SimpleRetryResiliencyPolicy() : base(4, "Simple")
    {
    }

    /// <summary>
    /// Gets the maximum number of retry attempts (2).
    /// </summary>
    public override int MaxRetries => 2;

    /// <summary>
    /// Gets the initial delay before the first retry (50ms).
    /// </summary>
    public override TimeSpan InitialDelay => TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Gets the maximum delay between retries (500ms).
    /// </summary>
    public override TimeSpan MaxDelay => TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets the backoff multiplier (2.0 - doubles delay each retry).
    /// </summary>
    public override double BackoffMultiplier => 2.0;

    /// <summary>
    /// Gets the circuit breaker open duration (15 seconds).
    /// </summary>
    public override TimeSpan CircuitBreakerDuration => TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets the number of failures to trip the circuit breaker (3).
    /// </summary>
    public override int CircuitBreakerThreshold => 3;

    /// <summary>
    /// Gets the resiliency category (Simple).
    /// </summary>
    public override IResiliencyCategory ResiliencyCategory => ResiliencyCategories.Simple;
}
