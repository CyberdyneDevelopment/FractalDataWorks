using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Operations.Data;

/// <summary>
/// Represents an escalation log entry tracked in the ops.EscalationLog table.
/// Provides an append-only audit trail of all escalation notifications.
/// </summary>
/// <remarks>
/// <para>
/// EscalationLog records every escalation attempt for compliance, debugging, and analysis:
/// <list type="bullet">
///   <item><description><strong>Audit Trail</strong> - Who was notified, when, and through which channel</description></item>
///   <item><description><strong>Success Tracking</strong> - Whether notification delivery succeeded</description></item>
///   <item><description><strong>Correlation</strong> - Links back to ExecutionItem and EscalationPolicy</description></item>
///   <item><description><strong>Troubleshooting</strong> - Error messages for failed notifications</description></item>
/// </list>
/// </para>
/// <para>
/// Each log entry captures:
/// <list type="bullet">
///   <item><description><strong>What</strong> - ExecutionItemId, EscalationPolicyId, Level</description></item>
///   <item><description><strong>When</strong> - Timestamp (when notification was attempted)</description></item>
///   <item><description><strong>How</strong> - NotificationChannel, Recipients</description></item>
///   <item><description><strong>Result</strong> - Success flag, NotificationId (from provider), ErrorMessage</description></item>
/// </list>
/// </para>
/// <para>
/// Use cases:
/// <list type="bullet">
///   <item><description>Verify escalation policy is working correctly</description></item>
///   <item><description>Investigate why notifications weren't received</description></item>
///   <item><description>Analyze escalation frequency and patterns</description></item>
///   <item><description>Demonstrate compliance with SLA notification requirements</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class EscalationLog
{
    /// <summary>
    /// Gets or sets the unique identifier for this log entry.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the identifier of the execution item that triggered escalation.
    /// Foreign key to ops.ExecutionItem(Id).
    /// </summary>
    /// <remarks>
    /// Links this log entry back to the specific workflow/job/step that failed.
    /// Essential for correlating escalations with execution history.
    /// </remarks>
    public Guid ExecutionItemId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the escalation policy that was applied.
    /// Foreign key to ops.EscalationPolicy(Id).
    /// </summary>
    /// <remarks>
    /// Identifies which policy triggered this notification.
    /// Useful for analyzing policy effectiveness and troubleshooting configuration.
    /// </remarks>
    public Guid EscalationPolicyId { get; set; }

    /// <summary>
    /// Gets or sets the escalation level that was triggered (1, 2, 3, etc.).
    /// </summary>
    /// <remarks>
    /// Indicates which tier of escalation was executed.
    /// Level 1 = initial notification, higher levels = more urgent.
    /// </remarks>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the notification channel used for this escalation.
    /// </summary>
    /// <value>
    /// Examples: "Email", "Teams", "PagerDuty", "Webhook".
    /// </value>
    /// <remarks>
    /// Records which communication channel was used.
    /// Matches NotificationChannel from the associated EscalationLevel configuration.
    /// </remarks>
    public string NotificationChannel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipients who were notified (as JSON array).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stores the actual recipients at the time of notification (snapshot).
    /// Format varies by channel:
    /// <list type="bullet">
    ///   <item><description>Email: ["user1@example.com", "user2@example.com"]</description></item>
    ///   <item><description>Teams: ["https://webhook.url"]</description></item>
    ///   <item><description>PagerDuty: ["integration-key"]</description></item>
    /// </list>
    /// </para>
    /// Captured at notification time in case configuration changes later.
    /// </remarks>
    public string Recipients { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the notification was delivered successfully.
    /// </summary>
    /// <remarks>
    /// <para>
    /// True indicates the notification provider confirmed delivery.
    /// False indicates delivery failed (see ErrorMessage for details).
    /// </para>
    /// <para>
    /// Note: "Delivered" means accepted by the notification service, not necessarily read by recipients.
    /// For example:
    /// <list type="bullet">
    ///   <item><description>Email: Accepted by SMTP server (may still bounce later)</description></item>
    ///   <item><description>Teams: Webhook returned 200 OK</description></item>
    ///   <item><description>PagerDuty: Incident created successfully</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the notification identifier returned by the provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provider-specific reference for tracking notification delivery:
    /// <list type="bullet">
    ///   <item><description>Email: Message-ID from SMTP</description></item>
    ///   <item><description>Teams: Activity ID from webhook response</description></item>
    ///   <item><description>PagerDuty: Incident ID or deduplication key</description></item>
    ///   <item><description>Custom webhooks: Response correlation ID</description></item>
    /// </list>
    /// </para>
    /// Null if notification failed or provider doesn't return an ID.
    /// Use for external correlation and troubleshooting.
    /// </remarks>
    public string? NotificationId { get; set; }

    /// <summary>
    /// Gets or sets the error message if notification delivery failed.
    /// Null if Success is true.
    /// </summary>
    /// <remarks>
    /// Contains technical details about the failure:
    /// - Network errors
    /// - Authentication failures
    /// - Invalid recipient addresses
    /// - Rate limit exceeded
    /// - Provider service outages
    /// Used for troubleshooting notification issues.
    /// </remarks>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this escalation was attempted.
    /// </summary>
    /// <remarks>
    /// Always UTC. Records when the notification was sent, not when it was received.
    /// Used for chronological analysis and verifying escalation timing.
    /// </remarks>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
