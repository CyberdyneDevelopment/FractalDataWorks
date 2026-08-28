using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that validation failed.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[Message("ValidationFailed")]
[MessageOption(typeof(ConnectionMessageCollectionBase))]
public sealed class ValidationFailedMessage : ConnectionMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedMessage"/> class.
    /// </summary>
    /// <param name="errorMessage">The validation error message.</param>
    public ValidationFailedMessage(string errorMessage)
        : base(1007, "ValidationFailed", MessageSeverity.Error,
               errorMessage, "CONN_VALIDATION_FAILED")
    { }
}
