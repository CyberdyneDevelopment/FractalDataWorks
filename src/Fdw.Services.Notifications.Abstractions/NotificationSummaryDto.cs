using System;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Summary DTO for a notification configuration, used in list views.
/// </summary>
public sealed class NotificationSummaryDto
{
    /// <summary>Gets or sets the notification unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the notification name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the service option type (e.g., Email, Webhook, Console).</summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the notification is enabled.</summary>
    public bool IsEnabled { get; set; }
}
