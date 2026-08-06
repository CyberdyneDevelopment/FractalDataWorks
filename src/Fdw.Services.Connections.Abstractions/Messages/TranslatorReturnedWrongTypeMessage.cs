using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// Message indicating that the translator returned an unexpected command type.
/// </summary>
[Message("TranslatorReturnedWrongType")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class TranslatorReturnedWrongTypeMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorReturnedWrongTypeMessage"/> class.
    /// </summary>
    public TranslatorReturnedWrongTypeMessage()
        : base(
            id: 3011,
            name: "TranslatorReturnedWrongType",
            severity: MessageSeverity.Error,
            message: "Translator returned '{0}' but expected '{1}'",
            code: "CONN_WRONG_COMMAND_TYPE")
    {
    }
}
