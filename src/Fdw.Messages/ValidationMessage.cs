using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Messages.Attributes;

namespace Fdw.Messages;

/// <summary>
/// Standard error message for validation failures.
/// </summary>
[Message("StandardMessages")]
[MessageOption(typeof(MessageCollectionBase<GenericMessage>))]
[ExcludeFromCodeCoverage]
public sealed class ValidationMessage : MessageTemplate<MessageSeverity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationMessage"/> class.
    /// </summary>
    /// <param name="validationError">Description of the validation error.</param>
    public ValidationMessage(string validationError)
        : base(
            id: 1004,
            name: "Validation",
            severity: MessageSeverity.Error,
            message: validationError,
            code: "VALIDATION",
            source: "Validation")
    {
        ValidationError = validationError;
    }

    /// <summary>
    /// Gets the description of the validation error.
    /// </summary>
    public string ValidationError { get; }
}