using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Email notification channel.
/// Sends notifications via SMTP email.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationChannels), "Email", RestrictToCurrentCompilation = true)]
public sealed class EmailChannel : NotificationChannelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailChannel"/> class.
    /// </summary>
    public EmailChannel()
        : base(
            id: 1,
            name: "Email",
            description: "SMTP email notifications",
            supportsBatchSend: true,
            supportsRichContent: true,
            supportsAttachments: true,
            maxMessageLength: null)
    {
    }
}
