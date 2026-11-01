using System;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Context for evaluating notification conditions.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class NotificationContext
{
    /// <summary>
    /// Gets or sets the execution status (e.g., "Failed", "Succeeded").
    /// </summary>
    public string? ExecutionStatus { get; set; }

    /// <summary>
    /// Gets or sets the current retry count.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the consecutive failure count.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets the execution duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the expected duration threshold.
    /// </summary>
    public TimeSpan? ExpectedDuration { get; set; }

    /// <summary>
    /// Gets or sets the threshold value from the condition configuration.
    /// </summary>
    public int? Threshold { get; set; }

    /// <summary>
    /// Gets or sets the duration threshold in ticks from the condition configuration.
    /// </summary>
    public long? DurationTicks { get; set; }

    /// <summary>
    /// Gets or sets the field name for value-based conditions.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the operator for value-based conditions.
    /// </summary>
    public string? Operator { get; set; }

    /// <summary>
    /// Gets or sets the comparison value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the actual field value for comparison.
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// Gets or sets whether the condition should be negated.
    /// </summary>
    public bool IsNegated { get; set; }
}
