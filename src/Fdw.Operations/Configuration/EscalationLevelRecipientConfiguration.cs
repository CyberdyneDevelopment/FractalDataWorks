using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>
/// Configuration for an individual escalation recipient.
/// Child of EscalationLevelConfiguration.
/// </summary>
/// <remarks>
/// <para>
/// Represents a single recipient for an escalation level notification.
/// Format varies by the parent level's NotificationChannel:
/// <list type="bullet">
///   <item><description><strong>Email</strong>: Recipient = "user@example.com", RecipientType = "Email"</description></item>
///   <item><description><strong>Teams</strong>: Recipient = "https://outlook.office.com/webhook/...", RecipientType = "Webhook"</description></item>
///   <item><description><strong>Slack</strong>: Recipient = "#alerts", RecipientType = "Channel"</description></item>
///   <item><description><strong>PagerDuty</strong>: Recipient = "integration-key", RecipientType = "IntegrationKey"</description></item>
/// </list>
/// </para>
/// <para>
/// Replaces the JSON string array from EscalationLevelConfiguration.Recipients
/// with proper relational structure for better validation and querying.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Operations")]
public sealed partial class EscalationLevelRecipientConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Operationss";

    /// <inheritdoc />
    public string ServiceType => "Operations";

    /// <inheritdoc />
    public string? ServiceOptionType => null;


    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the escalation level this recipient belongs to.
    /// Foreign key to workflow.EscalationLevel(Id).
    /// </summary>
    public Guid EscalationLevelId { get; set; }

    /// <summary>
    /// Gets or sets the name for display/binding purposes.
    /// </summary>
    /// <value>
    /// Human-readable identifier for the recipient.
    /// Examples: "Primary Team", "On-Call Engineer", "Alerts Channel", "Incident Webhook".
    /// </value>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient address (email, Slack channel, webhook URL, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Format depends on RecipientType:
    /// <list type="bullet">
    ///   <item><description><strong>Email</strong>: "user@example.com"</description></item>
    ///   <item><description><strong>Channel</strong>: "#alerts" or "C12345678" (Slack channel ID)</description></item>
    ///   <item><description><strong>Webhook</strong>: "https://api.example.com/webhook"</description></item>
    ///   <item><description><strong>IntegrationKey</strong>: "pd-integration-key-abc123"</description></item>
    ///   <item><description><strong>PhoneNumber</strong>: "+1-555-123-4567"</description></item>
    /// </list>
    /// </para>
    /// Validation is deferred to the notification service based on RecipientType.
    /// </remarks>
    public string Recipient { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipient type (Email, Slack, Teams, etc.).
    /// </summary>
    /// <value>
    /// Common types: "Email", "Webhook", "Channel", "IntegrationKey", "PhoneNumber".
    /// Default is "Email" for backward compatibility.
    /// </value>
    /// <remarks>
    /// <para>
    /// Determines how to interpret the Recipient address and which notification service to use.
    /// </para>
    /// Consider using TypeCollection lookup for type validation in future iterations.
    /// </remarks>
    public string RecipientType { get; set; } = "Email";
}
