namespace Fdw.Services.Notifications.Clients.Models;

/// <summary>
/// Represents a single notification preference for a user.
/// </summary>
public sealed class UserNotificationPreferenceResponse
{
    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the delivery channel.
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this preference is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
}
