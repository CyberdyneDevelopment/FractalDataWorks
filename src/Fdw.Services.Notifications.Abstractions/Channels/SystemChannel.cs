using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// System notification channel for in-app messages.
/// Bridges the notification service domain to the messaging system,
/// delivering notifications as in-system messages with lifecycle tracking.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
// Why not "System": the option name becomes a member on the generated collection, and a member named
// System shadows the System namespace inside that class — every emitted System.* reference in the
// collection then binds to this member instead.
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
