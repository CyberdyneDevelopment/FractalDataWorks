using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// Message indicating that command translation failed.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("TranslationFailed")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class TranslationFailedMessage : ConnectionMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationFailedMessage"/> class.
    /// </summary>
    public TranslationFailedMessage()
        : base(
            id: 3010,
            name: "TranslationFailed",
            severity: MessageSeverity.Error,
            message: "Failed to translate command",
            code: "CONN_TRANSLATION_FAILED")
    {
    }
}
