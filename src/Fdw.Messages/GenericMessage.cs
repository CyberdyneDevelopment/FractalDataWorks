using System;
using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Messages;

/// <summary>
/// Implementation of IGenericMessage for framework messages.
/// </summary>
public class GenericMessage : IGenericMessage, ITypeOption<int>
{

    /// <summary>
    /// Gets or sets the severity level of the message.
    /// </summary>
    public MessageSeverity Severity { get; set; } = MessageSeverity.Information;

    /// <inheritdoc/>
    public string Message { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Code { get; set; }

    /// <inheritdoc/>
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this message.
    /// </summary>
    public int Id { get; set; } = 1;

    object ITypeOption.Id => Id;

    /// <summary>
    /// Gets or sets the display name or string representation of this enum value.
    /// </summary>
    public string Name { get; set; } = "GenericMessage";

    /// <summary>
    /// Gets the category for this type option.
    /// </summary>
    public string Category => "Message";

    /// <summary>
    /// Gets or sets the timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the correlation identifier for related messages.
    /// </summary>
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets additional metadata for the message.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericMessage"/> class.
    /// </summary>
    public GenericMessage()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericMessage"/> class with a message.
    /// </summary>
    /// <param name="message">The message text.</param>
    public GenericMessage(string message)
    {
        Message = message ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericMessage"/> class with full details.
    /// </summary>
    /// <param name="severity">The message severity.</param>
    /// <param name="message">The message text.</param>
    /// <param name="code">The message code.</param>
    /// <param name="source">The message source.</param>
    public GenericMessage(MessageSeverity severity, string message, string? code = null, string? source = null)
    {
        Severity = severity;
        Message = message ?? string.Empty;
        Code = code;
        Source = source;
    }

    /// <inheritdoc/>
    public override string ToString() => Message;

    /// <summary>
    /// Creates a new GenericMessage instance. Used by source generators.
    /// </summary>
    /// <param name="severity">The message severity.</param>
    /// <param name="message">The message text.</param>
    /// <param name="code">The message code.</param>
    /// <param name="source">The message source.</param>
    /// <returns>A new GenericMessage instance.</returns>
    /// <remarks>
    /// This factory method exists for use by the MessageLogging source generator.
    /// Production code should use MessageLogging attributes or ResultCodes instead
    /// of calling this method directly.
    /// </remarks>
    public static GenericMessage Create(MessageSeverity severity, string message, string? code, string? source)
    {
        return new GenericMessage(severity, message, code, source);
    }

}
