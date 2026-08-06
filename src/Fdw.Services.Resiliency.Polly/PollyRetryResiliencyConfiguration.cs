using Fdw.Configuration;
using Fdw.Services.Resiliency;

namespace Fdw.Services.Resiliency.Polly;

/// <summary>
/// Configuration for the PollyRetry resiliency strategy.
/// Fields map to the <c>settings.PollyRetryResiliency</c> database table.
/// </summary>
public sealed class PollyRetryResiliencyConfiguration : ResiliencyConfiguration
{
    /// <inheritdoc/>
    public override string SectionName => "Resiliency:PollyRetry";

    /// <inheritdoc/>
    public override string StrategyType => "PollyRetry";

    /// <summary>
    /// Gets or sets the maximum number of retry attempts (not counting the initial attempt).
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the backoff kind: Exponential, Fixed, or Random.
    /// </summary>
    public string BackoffKind { get; set; } = "Exponential";

    /// <summary>
    /// Gets or sets the base delay in milliseconds before the first retry.
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds (caps exponential growth).
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the optional jitter percentage (0-100) to add randomness to delays.
    /// </summary>
    public int? JitterPercent { get; set; }

    /// <summary>
    /// Gets or sets the optional circuit-breaker failure threshold.
    /// When non-null, the circuit opens after this many consecutive failures.
    /// </summary>
    public int? CircuitBreakerThreshold { get; set; }

    /// <summary>
    /// Gets or sets the optional per-attempt timeout in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}
