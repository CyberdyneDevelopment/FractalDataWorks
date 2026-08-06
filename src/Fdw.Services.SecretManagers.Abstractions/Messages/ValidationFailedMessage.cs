using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that validation failed.
/// </summary>
// Why: pure message DTO; ctor only forwards literal id/severity/text to the base template, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ValidationFailed")]
[MessageOption(typeof(SecretManagerMessageCollectionBase))]
public sealed class ValidationFailedMessage : SecretManagerMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedMessage"/> class.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public ValidationFailedMessage(string errorMessage)
        : base(1004, "ValidationFailed", MessageSeverity.Error,
               errorMessage, "SM_VALIDATION_FAILED")
    { }
}
