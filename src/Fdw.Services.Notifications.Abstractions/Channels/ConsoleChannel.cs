using Fdw.Collections.Attributes;

namespace Fdw.Services.Notifications.Abstractions;

/// <summary>
/// Console/log notification channel for development and test environments.
/// Emits notification content via structured logging at Information level.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationChannels), "Console", RestrictToCurrentCompilation = true)]
public sealed class ConsoleChannel : NotificationChannelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleChannel"/> class.
    /// </summary>
    public ConsoleChannel()
        : base(
            id: 4,
            name: "Console",
            description: "Development/test channel that logs notification content via structured logging",
            supportsBatchSend: true,
            supportsRichContent: false,
            supportsAttachments: false,
            maxMessageLength: null)
    {
    }
}
