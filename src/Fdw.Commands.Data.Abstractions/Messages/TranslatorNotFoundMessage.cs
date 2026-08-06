using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Message indicating that a translator was not found.
/// </summary>
[Message("TranslatorNotFound")]
public sealed class TranslatorNotFoundMessage : DataCommandMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorNotFoundMessage"/> class.
    /// </summary>
    public TranslatorNotFoundMessage()
        : base(
            id: 101,
            name: "TranslatorNotFound",
            severity: MessageSeverity.Error,
            message: "Translator not found",
            code: "DATACMD_101")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorNotFoundMessage"/> class with translator name.
    /// </summary>
    /// <param name="translatorName">The name of the translator that was not found.</param>
    public TranslatorNotFoundMessage(string translatorName)
        : base(
            id: 101,
            name: "TranslatorNotFound",
            severity: MessageSeverity.Error,
            message: string.Format(System.Globalization.CultureInfo.InvariantCulture, "Translator '{0}' not found", translatorName),
            code: "DATACMD_101")
    {
    }
}
