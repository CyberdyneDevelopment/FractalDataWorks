using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Microsoft Teams notification channel.
/// Sends notifications via Microsoft Teams webhook.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationChannels), "Teams", RestrictToCurrentCompilation = true)]
public sealed class TeamsChannel : NotificationChannelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsChannel"/> class.
    /// </summary>
    public TeamsChannel()
        : base(
            id: 2,
            name: "Teams",
            description: "Microsoft Teams webhook notifications",
            supportsBatchSend: false,
            supportsRichContent: true,
            supportsAttachments: false,
            maxMessageLength: 28000) // Teams card limit
    {
    }
}
