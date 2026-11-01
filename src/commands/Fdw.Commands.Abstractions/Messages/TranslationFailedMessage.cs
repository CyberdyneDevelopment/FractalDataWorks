using Fdw.Messages;

namespace Fdw.Commands.Abstractions.Messages;

/// <summary>
/// Message for when command translation fails.
/// </summary>
public sealed class TranslationFailedMessage : CommandMessage
{
    /// <summary>
    /// Gets the reason for translation failure.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationFailedMessage"/> class.
    /// </summary>
    /// <param name="reason">The reason for translation failure.</param>
    public TranslationFailedMessage(string reason)
        : base(1002, "TranslationFailed", MessageSeverity.Error,
               $"Failed to translate command: {reason}", "CMD_TRANS_001")
    {
        Reason = reason;
    }
}