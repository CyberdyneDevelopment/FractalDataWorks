namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Domain model for a single user notification preference entry
/// (a notification-type / delivery-channel pair and its enabled state).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class NotificationPreference
{
    /// <summary>Gets the notification type (e.g. PipelineFailure, ScheduleTrigger).</summary>
    public required string NotificationType { get; init; }

    /// <summary>Gets the delivery channel (e.g. InApp, Email, Webhook).</summary>
    public required string Channel { get; init; }

    /// <summary>Gets a value indicating whether this preference is enabled.</summary>
    public required bool IsEnabled { get; init; }
}
