using System;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// Thrown by a schema entry's converter when the entry is refused for a reason that has a coded message of its own (an implementation no option registered),
/// because a converter cannot return a result. <see cref="ConfigurationSchemaLoader"/> catches it by its type and answers <see cref="Refusal"/> as the failure
/// (FDW-6433); the text is for a reader that never reaches the loader.
/// </summary>
/// <remarks>
/// A converter has no logger, so it carries the way to make the message and the loader makes it with the logger it holds: the message is logged once, where the
/// host can see it. An exception made without that way (the standard constructors) carries none, and the loader answers it as a file that did not bind. It is an
/// <see cref="Exception"/> and not a JSON exception on purpose: the serializer wraps what is a JSON exception on its way out of a converter, and an exception of
/// another kind reaches the loader as it was thrown.
/// </remarks>
internal sealed class SchemaRefusalException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SchemaRefusalException"/> class.</summary>
    public SchemaRefusalException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SchemaRefusalException"/> class.</summary>
    /// <param name="message">What the exception says.</param>
    public SchemaRefusalException(string? message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SchemaRefusalException"/> class.</summary>
    /// <param name="message">What the exception says.</param>
    /// <param name="innerException">What caused it.</param>
    public SchemaRefusalException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SchemaRefusalException"/> class.</summary>
    /// <param name="message">What the exception says.</param>
    /// <param name="refusal">Makes the coded message from the logger the loader holds.</param>
    public SchemaRefusalException(string? message, Func<ILogger, IGenericMessage> refusal)
        : base(message)
    {
        Refusal = refusal;
    }

    /// <summary>Gets the way to make the coded message, given the logger to record it with; none for an exception made without one.</summary>
    public Func<ILogger, IGenericMessage>? Refusal { get; }
}
