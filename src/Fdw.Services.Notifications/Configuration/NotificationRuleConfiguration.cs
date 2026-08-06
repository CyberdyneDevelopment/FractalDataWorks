using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Configuration.Abstractions;
using Fdw.Data;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>
/// Configuration for notification rules.
/// Generates the table <c>notify.NotificationRule</c>.
/// </summary>
/// <remarks>
/// <para>
/// A notification rule defines when to send notifications based on execution events.
/// Rules are associated with schedules, pipelines, or workflows and triggered when
/// their conditions are met.
/// </para>
/// <para>
/// Rule evaluation:
/// <list type="bullet">
/// <item><description>Multiple conditions can be combined with AND/OR logic</description></item>
/// <item><description>Conditions are evaluated after each execution event</description></item>
/// <item><description>Notification is sent via the configured service type and name</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification")]
public partial class NotificationRuleConfiguration : ConfigurationBase<NotificationRuleConfiguration>
{
    /// <inheritdoc />
    public override string SectionName => "Notification";

    /// <inheritdoc />
    public override string ServiceType => "NotificationRule";

    /// <summary>
    /// Gets or sets the optional description of this rule.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this rule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the schedule ID this rule applies to (cross-domain FK).
    /// Null if rule is not schedule-specific.
    /// </summary>
    public Guid? ScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the pipeline ID this rule applies to (cross-domain FK).
    /// Null if rule is not pipeline-specific.
    /// </summary>
    public Guid? PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the workflow ID this rule applies to (cross-domain FK).
    /// Null if rule is not workflow-specific.
    /// </summary>
    public Guid? WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets how multiple conditions are combined.
    /// </summary>
    /// <value>"And" or "Or" (default is "Or")</value>
    public string ConditionOperator { get; set; } = "Or";

    /// <summary>
    /// Gets or sets the notification service type to use (e.g., "Email", "Slack", "Teams").
    /// </summary>
    public string NotificationServiceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification service name (configured instance).
    /// </summary>
    public string NotificationServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message template for the notification.
    /// Supports placeholders like {PipelineName}, {ExecutionStatus}, {ErrorMessage}.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Gets or sets the severity level for this rule.
    /// </summary>
    /// <value>"Info", "Warning", "Error", or "Critical"</value>
    public string Severity { get; set; } = "Info";

    /// <summary>
    /// Gets or sets the minimum interval between notifications in minutes (cooldown).
    /// Prevents notification flooding for recurring failures.
    /// </summary>
    public int? CooldownMinutes { get; set; }

    /// <summary>
    /// Gets or sets when the last notification was sent (for cooldown tracking).
    /// </summary>
    public DateTimeOffset? LastNotificationSent { get; set; }
}
