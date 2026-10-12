namespace Fdw.Operations.Clients;

/// <summary>
/// Payload for a notification preference entry.
/// </summary>
public sealed class NotificationPreferencePayload
{
    /// <summary>Gets or sets the notification type (e.g., PipelineFailure, ScheduleTrigger).</summary>
    public required string NotificationType { get; set; }

    /// <summary>Gets or sets the delivery channel (e.g., InApp, Email, Webhook).</summary>
    public required string Channel { get; set; }

    /// <summary>Gets or sets whether this preference is enabled.</summary>
    public bool IsEnabled { get; set; }
}
