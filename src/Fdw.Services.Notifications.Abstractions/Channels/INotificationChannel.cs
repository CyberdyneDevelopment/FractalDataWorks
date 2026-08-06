using Fdw.Collections;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Interface for notification channels.
/// Defines the contract for notification delivery channels (Email, Teams, Webhook, etc.).
/// </summary>
public interface INotificationChannel : ITypeOption<int>
{
    /// <summary>
    /// Gets whether this channel supports batch sending.
    /// </summary>
    bool SupportsBatchSend { get; }

    /// <summary>
    /// Gets whether this channel supports rich content (HTML, markdown).
    /// </summary>
    bool SupportsRichContent { get; }

    /// <summary>
    /// Gets whether this channel supports attachments.
    /// </summary>
    bool SupportsAttachments { get; }

    /// <summary>
    /// Gets the maximum message length supported by this channel.
    /// Null means unlimited.
    /// </summary>
    int? MaxMessageLength { get; }
}
