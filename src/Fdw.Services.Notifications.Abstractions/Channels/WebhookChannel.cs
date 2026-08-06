using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Generic webhook notification channel.
/// Sends notifications via HTTP webhook to custom endpoints.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationChannels), "Webhook", RestrictToCurrentCompilation = true)]
public sealed class WebhookChannel : NotificationChannelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookChannel"/> class.
    /// </summary>
    public WebhookChannel()
        : base(
            id: 3,
            name: "Webhook",
            description: "Generic HTTP webhook notifications",
            supportsBatchSend: true,
            supportsRichContent: true,
            supportsAttachments: false,
            maxMessageLength: null)
    {
    }
}
