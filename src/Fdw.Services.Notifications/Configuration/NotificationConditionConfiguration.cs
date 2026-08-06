using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>
/// Configuration for notification conditions.
/// Generates the table <c>notify.NotificationCondition</c> as a child of <c>notify.NotificationRule</c>.
/// </summary>
/// <remarks>
/// <para>
/// Conditions define when a notification rule should fire. Multiple conditions can be
/// combined using the parent rule's ConditionOperator (And/Or).
/// </para>
/// <para>
/// Supported condition types:
/// <list type="bullet">
/// <item><description>RetryThreshold - Fires when retry count exceeds threshold</description></item>
/// <item><description>ConsecutiveFailures - Fires after N consecutive failures</description></item>
/// <item><description>TimeWindow - Fires if failure occurs within time window</description></item>
/// <item><description>DurationExceeded - Fires if execution exceeds expected duration</description></item>
/// <item><description>ValueCondition - Fires based on field value comparison</description></item>
/// <item><description>ExecutionStatus - Fires on specific status (Failed, Succeeded, etc.)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification")]
public partial class NotificationConditionConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this condition.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for this condition (format: {RuleId}:{ConditionType}:{Ordinal}).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification rule ID this condition belongs to.
    /// Generated automatically by ParentTableName - excluded from DDL to prevent duplicate.
    /// </summary>
    [NotMapped]
    public Guid NotificationRuleId { get; set; }

    /// <summary>
    /// Gets or sets the condition type.
    /// </summary>
    /// <value>
    /// One of: "RetryThreshold", "ConsecutiveFailures", "TimeWindow",
    /// "DurationExceeded", "ValueCondition", "ExecutionStatus"
    /// </value>
    public string ConditionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the threshold value for threshold-based conditions.
    /// Used by: RetryThreshold, ConsecutiveFailures
    /// </summary>
    public int? Threshold { get; set; }

    /// <summary>
    /// Gets or sets the duration in ticks for time-based conditions.
    /// Used by: TimeWindow, DurationExceeded
    /// </summary>
    public long? DurationTicks { get; set; }

    /// <summary>
    /// Gets or sets the field name for value-based conditions.
    /// Used by: ValueCondition
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the comparison operator for value-based conditions.
    /// </summary>
    /// <value>One of: "Equal", "NotEqual", "GreaterThan", "LessThan", "Contains", "StartsWith"</value>
    public string? Operator { get; set; }

    /// <summary>
    /// Gets or sets the comparison value for value-based conditions.
    /// Used by: ValueCondition, ExecutionStatus
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets a custom expression for complex conditions.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position for condition evaluation order.
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets whether to negate this condition.
    /// </summary>
    public bool IsNegated { get; set; }
}
