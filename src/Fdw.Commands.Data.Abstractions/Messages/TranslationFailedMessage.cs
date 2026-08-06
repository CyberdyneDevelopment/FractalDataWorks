using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Message indicating that command translation failed.
/// </summary>
[Message("TranslationFailed")]
public sealed class TranslationFailedMessage : DataCommandMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationFailedMessage"/> class.
    /// </summary>
    public TranslationFailedMessage()
        : base(
            id: 100,
            name: "TranslationFailed",
            severity: MessageSeverity.Error,
            message: "Failed to translate command",
            code: "DATACMD_100")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationFailedMessage"/> class with command type.
    /// </summary>
    /// <param name="commandType">The type of command that failed to translate.</param>
    public TranslationFailedMessage(string commandType)
        : base(
            id: 100,
            name: "TranslationFailed",
            severity: MessageSeverity.Error,
            message: string.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to translate {0} command", commandType),
            code: "DATACMD_100")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationFailedMessage"/> class with command type and error.
    /// </summary>
    /// <param name="commandType">The type of command that failed to translate.</param>
    /// <param name="error">The error message.</param>
    public TranslationFailedMessage(string commandType, string error)
        : base(
            id: 100,
            name: "TranslationFailed",
            severity: MessageSeverity.Error,
            message: string.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to translate {0} command: {1}", commandType, error),
            code: "DATACMD_100")
    {
    }
}
