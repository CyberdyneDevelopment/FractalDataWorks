using System;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Resiliency.Abstractions.Policies;

/// <summary>
/// Resiliency policy optimized for database operations.
/// Designed to handle transient connection issues, deadlocks, and timeout scenarios
/// common in database operations.
/// </summary>
/// <remarks>
/// <para>
/// This policy uses moderate retry counts with exponential backoff to allow
/// the database server time to recover from temporary issues while preventing
/// overwhelming the server with retry attempts.
/// </para>
/// <para>
/// Circuit breaker settings are tuned for database scenarios where sustained
/// failures typically indicate a more serious issue requiring intervention.
/// </para>
/// </remarks>
[TypeOption(typeof(ResiliencyPolicies), "Database", RestrictToCurrentCompilation = true)]
public sealed class DatabaseResiliencyPolicy : ResiliencyPolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseResiliencyPolicy"/> class.
    /// </summary>
    public DatabaseResiliencyPolicy() : base(1, "Database")
    {
    }

    /// <summary>
    /// Gets the maximum number of retry attempts (3).
    /// </summary>
    public override int MaxRetries => 3;

    /// <summary>
    /// Gets the initial delay before the first retry (100ms).
    /// </summary>
    public override TimeSpan InitialDelay => TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets the maximum delay between retries (5 seconds).
    /// </summary>
    public override TimeSpan MaxDelay => TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the backoff multiplier (2.0 - doubles delay each retry).
    /// </summary>
    public override double BackoffMultiplier => 2.0;

    /// <summary>
    /// Gets the circuit breaker open duration (30 seconds).
    /// </summary>
    public override TimeSpan CircuitBreakerDuration => TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the number of failures to trip the circuit breaker (5).
    /// </summary>
    public override int CircuitBreakerThreshold => 5;

    /// <summary>
    /// Gets the resiliency category (Database).
    /// </summary>
    public override IResiliencyCategory ResiliencyCategory => ResiliencyCategories.Database;
}
