using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>
/// Configuration for individual escalation levels within an escalation policy.
/// Defines delay, recipients, channel, and severity for a specific escalation tier.
/// </summary>
/// <remarks>
/// <para>
/// Escalation levels form a progressive notification ladder:
/// <list type="bullet">
///   <item><description><strong>Level 1</strong> - Initial notification (usually immediate or short delay)</description></item>
///   <item><description><strong>Level 2</strong> - Secondary notification (if issue persists)</description></item>
///   <item><description><strong>Level 3+</strong> - Higher severity notifications (management, on-call, incident response)</description></item>
/// </list>
/// </para>
/// <para>
/// Each level specifies:
/// <list type="bullet">
///   <item><description><strong>When</strong> - DelayMinutes from policy trigger</description></item>
///   <item><description><strong>Who</strong> - Recipients (email addresses, Teams channels, webhook URLs)</description></item>
///   <item><description><strong>How</strong> - NotificationChannel (Email, Teams, Webhook, PagerDuty)</description></item>
///   <item><description><strong>What</strong> - Severity (Info, Warning, Error, Critical)</description></item>
///   <item><description><strong>Template</strong> - Optional notification template for consistent formatting</description></item>
/// </list>
/// </para>
/// <para>
/// Example three-level escalation:
/// <list type="number">
///   <item><description>Level 1: 0 min delay, Email to team@example.com, Warning</description></item>
///   <item><description>Level 2: 15 min delay, Teams to #alerts channel, Error</description></item>
///   <item><description>Level 3: 60 min delay, PagerDuty to on-call, Critical</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Operations")]
public sealed partial class EscalationLevelConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Operationss";

    /// <inheritdoc />
    public string ServiceType => "Operations";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the display name for this escalation level.
    /// </summary>
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the unique identifier for this escalation level.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the escalation policy this level belongs to.
    /// Foreign key to ops.EscalationPolicy(Id).
    /// </summary>
    public Guid EscalationPolicyId { get; set; }

    /// <summary>
    /// Gets or sets the escalation tier number (1, 2, 3, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Determines the order of escalation:
    /// <list type="bullet">
    ///   <item><description>Level 1 triggers first (usually immediate or short delay)</description></item>
    ///   <item><description>Level 2 triggers if issue persists past Level 1 delay</description></item>
    ///   <item><description>Level N continues until MaxEscalationLevel is reached</description></item>
    /// </list>
    /// </para>
    /// Must be between 1 and parent policy's MaxEscalationLevel.
    /// </remarks>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the delay in minutes before this level triggers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Measured from the initial policy trigger event (not from previous level).
    /// </para>
    /// <para>
    /// Example escalation timeline:
    /// <list type="bullet">
    ///   <item><description>Level 1: DelayMinutes = 0 → Triggers immediately</description></item>
    ///   <item><description>Level 2: DelayMinutes = 15 → Triggers at T+15 if unresolved</description></item>
    ///   <item><description>Level 3: DelayMinutes = 60 → Triggers at T+60 if unresolved</description></item>
    /// </list>
    /// </para>
    /// Typical values: 0 (immediate), 15, 30, 60, 120 minutes.
    /// </remarks>
    public int DelayMinutes { get; set; }

    /// <summary>
    /// Gets or sets the notification channel to use for this level.
    /// </summary>
    /// <value>
    /// Common channels: "Email", "Teams", "Slack", "Webhook", "PagerDuty", "SMS".
    /// </value>
    /// <remarks>
    /// <para>
    /// Determines which notification service handles delivery.
    /// Must match a registered notification service type.
    /// </para>
    /// Consider using ServiceTypeCollection lookup for channel validation in future iterations.
    /// </remarks>
    public string NotificationChannel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipients for this notification level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Collection of individual recipients for this escalation level.
    /// Each recipient specifies an address (email, channel, webhook, etc.) and type.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><description><strong>Email</strong>: Recipient = "user@example.com", RecipientType = "Email"</description></item>
    ///   <item><description><strong>Teams</strong>: Recipient = "https://outlook.office.com/webhook/...", RecipientType = "Webhook"</description></item>
    ///   <item><description><strong>PagerDuty</strong>: Recipient = "integration-key", RecipientType = "IntegrationKey"</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Uses proper relational structure for better validation, querying, and type safety.
    /// </para>
    /// </remarks>
    public IList<EscalationLevelRecipientConfiguration> Recipients { get; set; } = [];

    /// <summary>
    /// Gets or sets the optional notification template name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// References a pre-defined template for consistent message formatting.
    /// </para>
    /// <para>
    /// Templates can include placeholders for execution context:
    /// <list type="bullet">
    ///   <item><description>{{ExecutionItemName}}</description></item>
    ///   <item><description>{{State}}</description></item>
    ///   <item><description>{{ResultMessage}}</description></item>
    ///   <item><description>{{DurationMs}}</description></item>
    ///   <item><description>{{Timestamp}}</description></item>
    /// </list>
    /// </para>
    /// Null means use default template for the channel.
    /// Example: "CriticalFailureTemplate", "WarningNotificationTemplate".
    /// </remarks>
    public string? Template { get; set; }

    /// <summary>
    /// Gets or sets the severity level for this notification.
    /// </summary>
    /// <value>
    /// Standard values: "Info", "Warning", "Error", "Critical".
    /// </value>
    /// <remarks>
    /// <para>
    /// Influences notification presentation and routing:
    /// <list type="bullet">
    ///   <item><description><strong>Info</strong> - Informational (low priority)</description></item>
    ///   <item><description><strong>Warning</strong> - Potential issue (needs attention)</description></item>
    ///   <item><description><strong>Error</strong> - Actual failure (requires action)</description></item>
    ///   <item><description><strong>Critical</strong> - Severe failure (immediate response)</description></item>
    /// </list>
    /// </para>
    /// Typically escalates with level: Level 1 = Warning, Level 2 = Error, Level 3 = Critical.
    /// </remarks>
    public string Severity { get; set; } = "Warning";
}
