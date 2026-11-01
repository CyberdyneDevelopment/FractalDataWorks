using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Messages.Attributes;

namespace Fdw.Messages;

/// <summary>
/// Standard error message for argument null exceptions.
/// </summary>
[Message("StandardMessages")]
[MessageOption(typeof(MessageCollectionBase<GenericMessage>))]
[ExcludeFromCodeCoverage]
public sealed class ArgumentNullMessage : MessageTemplate<MessageSeverity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentNullMessage"/> class.
    /// </summary>
    /// <param name="parameterName">The name of the null parameter.</param>
    public ArgumentNullMessage(string parameterName)
        : base(
            id: 1001,
            name: "ArgumentNull",
            severity: MessageSeverity.Error,
            message: $"Parameter '{parameterName}' cannot be null",
            code: "ARG_NULL",
            source: "ArgumentValidation")
    {
        ParameterName = parameterName;
    }

    /// <summary>
    /// Gets the name of the parameter that was null.
    /// </summary>
    public string ParameterName { get; }
}