namespace Fdw.Services.Messaging.Components.Models;

/// <summary>
/// Model for a user's notification delivery preference per message type.
/// </summary>
public sealed class NotificationPreference
{
    public string MessageType { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public bool InSystem { get; set; } = true;

    public bool Email { get; set; }
}
