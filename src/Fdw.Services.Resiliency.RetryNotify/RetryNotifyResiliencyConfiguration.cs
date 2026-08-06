using Fdw.Configuration;
using System;
using Fdw.Services.Resiliency;

namespace Fdw.Services.Resiliency.RetryNotify;

/// <summary>
/// Configuration for the RetryNotify resiliency strategy.
/// Fields map to the <c>settings.RetryNotifyResiliency</c> database table.
/// </summary>
public sealed class RetryNotifyResiliencyConfiguration : ResiliencyConfiguration
{
    /// <inheritdoc/>
    public override string SectionName => "Resiliency:RetryNotify";

    /// <inheritdoc/>
    public override string StrategyType => "RetryNotify";

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
    /// Gets or sets the notification target identifier.
    /// Used to resolve the notification channel on terminal failure.
    /// </summary>
    public Guid NotificationTargetId { get; set; }
}
