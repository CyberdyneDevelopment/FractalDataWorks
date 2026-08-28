using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// System notification channel for in-app messages.
/// Bridges the notification service domain to the messaging system,
/// delivering notifications as in-system messages with lifecycle tracking.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationChannels), "SystemNotification", RestrictToCurrentCompilation = true)]
public sealed class SystemChannel : NotificationChannelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemChannel"/> class.
    /// </summary>
    public SystemChannel()
        : base(
            id: 5,
            name: "SystemNotification",
            description: "In-system message channel that delivers notifications as tracked messages",
            supportsBatchSend: true,
            supportsRichContent: true,
            supportsAttachments: false,
            maxMessageLength: null)
    {
    }
}
