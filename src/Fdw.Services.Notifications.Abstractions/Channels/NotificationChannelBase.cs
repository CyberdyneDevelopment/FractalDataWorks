using Fdw.Collections;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Base class for notification channels.
/// Implements the INotificationChannel interface with common functionality.
/// </summary>
public abstract class NotificationChannelBase : TypeOptionBase<NotificationChannelBase>, INotificationChannel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationChannelBase"/> class.
    /// </summary>
    /// <param name="name">The name of the notification channel.</param>
    /// <param name="description">A description of the notification channel.</param>
    /// <param name="supportsBatchSend">Whether this channel supports batch sending.</param>
    /// <param name="supportsRichContent">Whether this channel supports rich content.</param>
    /// <param name="supportsAttachments">Whether this channel supports attachments.</param>
    /// <param name="maxMessageLength">The maximum message length, or null for unlimited.</param>
    protected NotificationChannelBase(
        string name,
        string description,
        bool supportsBatchSend = true,
        bool supportsRichContent = true,
        bool supportsAttachments = false,
        int? maxMessageLength = null)
        : base(name, $"Notifications:{name}", name, description, "Notifications")
    {
        SupportsBatchSend = supportsBatchSend;
        SupportsRichContent = supportsRichContent;
        SupportsAttachments = supportsAttachments;
        MaxMessageLength = maxMessageLength;
    }

    /// <inheritdoc/>
    public bool SupportsBatchSend { get; }

    /// <inheritdoc/>
    public bool SupportsRichContent { get; }

    /// <inheritdoc/>
    public bool SupportsAttachments { get; }

    /// <inheritdoc/>
    public int? MaxMessageLength { get; }
}
