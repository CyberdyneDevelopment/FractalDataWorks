using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Notifications.Configuration;

/// <summary>
/// Configuration for notification recipients.
/// Generates the table <c>notify.NotificationRecipient</c> as a child of <c>notify.NotificationRule</c>.
/// </summary>
/// <remarks>
/// <para>
/// Recipients define who should receive notifications when a rule fires.
/// Each recipient has a type that determines how the recipient value is interpreted.
/// </para>
/// <para>
/// Recipient types:
/// <list type="bullet">
/// <item><description>Email - Email address (e.g., "oncall@company.com")</description></item>
/// <item><description>SlackChannel - Slack channel name (e.g., "#ops-alerts")</description></item>
/// <item><description>SlackUser - Slack user mention (e.g., "@john.doe")</description></item>
/// <item><description>TeamsChannel - Teams channel webhook URL or name</description></item>
/// <item><description>Webhook - HTTP endpoint URL for custom integrations</description></item>
/// <item><description>PagerDuty - PagerDuty service key</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Notification")]
public partial class NotificationRecipientConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this recipient.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for this recipient (format: {RecipientType}:{Recipient}).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification rule ID this recipient belongs to.
    /// Generated automatically by ParentTableName - excluded from DDL to prevent duplicate.
    /// </summary>
    [NotMapped]
    public Guid NotificationRuleId { get; set; }

    /// <summary>
    /// Gets or sets the recipient value (email, channel, URL, etc.).
    /// </summary>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of recipient.
    /// </summary>
    /// <value>
    /// One of: "Email", "SlackChannel", "SlackUser", "TeamsChannel", "Webhook", "PagerDuty"
    /// </value>
    public string RecipientType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for this recipient (for UI).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets whether this recipient is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the ordinal position for multiple recipients.
    /// </summary>
    public int Ordinal { get; set; }
}
