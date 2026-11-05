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


    /// <summary>
    /// Gets or sets the maximum number of retry attempts (not counting the initial attempt). Required: the policy's definer states it, and the strategy refuses a policy that does not (FDW-3931).
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the backoff kind: Exponential, Fixed, or Random. Required: the policy's definer states it, and the strategy refuses a policy that does not (FDW-3931).
    /// </summary>
    public string? BackoffKind { get; set; }

    /// <summary>
    /// Gets or sets the base delay in milliseconds before the first retry. Required: the policy's definer states it, and the strategy refuses a policy that does not (FDW-3931).
    /// </summary>
    public int? BaseDelayMs { get; set; }

    /// <summary>
    /// Gets or sets the longest wait between attempts, in milliseconds (caps exponential growth). The strategy held 30000 in code
    /// before FDW-3931. Required: the policy's definer states it, and the strategy refuses a policy that does not (FDW-3931).
    /// </summary>
    public int? MaxDelayMs { get; set; }

    /// <summary>
    /// Gets or sets the notification target identifier.
    /// Used to resolve the notification channel on terminal failure.
    /// </summary>
    public Guid NotificationTargetId { get; set; }
}
