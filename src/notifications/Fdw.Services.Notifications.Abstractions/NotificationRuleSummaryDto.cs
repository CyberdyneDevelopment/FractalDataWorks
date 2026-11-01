using System;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Summary DTO for a notification rule, used in list views.
/// </summary>
public sealed class NotificationRuleSummaryDto
{
    /// <summary>Gets or sets the rule unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the rule name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the rule is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the notification service type (e.g., Webhook, Email).</summary>
    public required string NotificationServiceType { get; set; }

    /// <summary>Gets or sets the notification service name.</summary>
    public required string NotificationServiceName { get; set; }

    /// <summary>Gets or sets the severity level.</summary>
    public required string Severity { get; set; }
}
