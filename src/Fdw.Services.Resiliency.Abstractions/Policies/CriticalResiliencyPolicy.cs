using System;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions.Policies;

/// <summary>
/// Resiliency policy for critical operations requiring aggressive retry behavior.
/// Designed for essential operations where failure is not an acceptable outcome
/// and maximum effort should be made to complete successfully.
/// </summary>
/// <remarks>
/// <para>
/// This policy uses the highest retry counts and longest timeout windows.
/// It is intended for operations that are essential to system function,
/// such as health check reports, audit logging, or critical data synchronization.
/// </para>
/// <para>
/// The circuit breaker has a higher threshold and longer recovery window,
/// reflecting the importance of attempting these operations even when
/// the system is under stress.
/// </para>
/// <para>
/// <b>Warning:</b> Use this policy sparingly. Excessive retries can contribute
/// to cascade failures if the underlying issue is systemic.
/// </para>
/// </remarks>
[TypeOption(typeof(ResiliencyPolicies), "Critical", RestrictToCurrentCompilation = true)]
public sealed class CriticalResiliencyPolicy : ResiliencyPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CriticalResiliencyPolicy"/> class.
    /// </summary>
    public CriticalResiliencyPolicy() : base(3, "Critical")
    {
    }

    /// <summary>
    /// Gets the maximum number of retry attempts (10).
    /// </summary>
    public override int MaxRetries => 10;

    /// <summary>
    /// Gets the initial delay before the first retry (500ms).
    /// </summary>
    public override TimeSpan InitialDelay => TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets the maximum delay between retries (2 minutes).
    /// </summary>
    public override TimeSpan MaxDelay => TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets the backoff multiplier (1.5 - increases delay by 50% each retry).
    /// </summary>
    public override double BackoffMultiplier => 1.5;

    /// <summary>
    /// Gets the circuit breaker open duration (5 minutes).
    /// </summary>
    public override TimeSpan CircuitBreakerDuration => TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the number of failures to trip the circuit breaker (20).
    /// </summary>
    public override int CircuitBreakerThreshold => 20;

    /// <summary>
    /// Gets the resiliency category (Critical).
    /// </summary>
    public override IResiliencyCategory ResiliencyCategory => ResiliencyCategories.Critical;
}
