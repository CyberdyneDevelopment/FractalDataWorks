using Fdw.Messages;
using Fdw.Messages.Attributes;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Messages;

/// <summary>
/// CurrentMessage indicating that validation failed.
/// </summary>
[Message("ValidationFailed")]
public sealed class ValidationFailedMessage : ServiceMessage, IServiceMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationFailedMessage"/> class.
    /// </summary>
    public ValidationFailedMessage()
        : base(1002, "ValidationFailed", MessageSeverity.Error,
               "Validation failed", "VALIDATION_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with validation error details.
    /// </summary>
    /// <param name="errors">The validation errors that occurred.</param>
    public ValidationFailedMessage(string errors)
        : base(1002, "ValidationFailed", MessageSeverity.Error,
               $"Validation failed: {errors}", "VALIDATION_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with field name and error details.
    /// </summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="error">The validation error message.</param>
    public ValidationFailedMessage(string fieldName, string error)
        : base(1002, "ValidationFailed", MessageSeverity.Error,
               $"Validation failed for {fieldName}: {error}", "VALIDATION_FAILED")
    { }
}