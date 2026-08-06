using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Collections;

namespace Fdw.Messages;

/// <summary>
/// Base class for framework messages that provides structured message functionality.
/// Provides structured message types with severity, code, source information, and formatting capabilities.
/// </summary>
public abstract class MessageTemplate<TSeverity> : IMessageIdentifier, IGenericMessage<TSeverity>, ITypeOption<int>
where TSeverity : Enum
{
    /// <summary>
    /// Gets the unique identifier for this message type.
    /// </summary>
    /// <value>The unique identifier for this message type.</value>
    public int Id { get; }

    object ITypeOption.Id => Id;

    /// <summary>
    /// Gets the name of this message type.
    /// </summary>
    /// <value>The name of this message type.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the category for this type option.
    /// </summary>
    public string Category => "Message";

    /// <summary>
    /// Gets the severity level of the message.
    /// </summary>
    /// <value>The severity level indicating the importance and impact of the message.</value>
    public TSeverity Severity { get; }

    /// <summary>
    /// The origin or context where the message was created.
    /// </summary>
    public string OriginatedIn { get; }

    /// <summary>
    /// Gets the message text.
    /// </summary>
    /// <value>The human-readable message text describing the condition or status.</value>
    public string Message { get; }

    /// <summary>
    /// Gets the message code or identifier.
    /// </summary>
    /// <value>A unique identifier for this type of message, useful for programmatic handling.</value>
    public string? Code { get; }

    /// <summary>
    /// Gets the source component or operation that generated the message.
    /// </summary>
    /// <value>The name or identifier of the source that generated this message.</value>
    public string? Source { get; }

    /// <summary>
    /// Gets the timestamp when this message was created.
    /// </summary>
    /// <value>The UTC timestamp of message creation.</value>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets additional details associated with this message.
    /// </summary>
    /// <value>A dictionary of additional details or metadata.</value>
    public IDictionary<string, object?>? Details { get; }

    /// <summary>
    /// Gets the data associated with this message instance.
    /// </summary>
    /// <value>Additional data object associated with the message.</value>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTemplate{T}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this message type.</param>
    /// <param name="name">The name of this message type.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="code">The message code or identifier (optional).</param>
    /// <param name="source">The source component or operation (optional).</param>
    protected MessageTemplate(int id, string name, TSeverity severity, string message, string? code = null, string? source = null)
    {
        Id = id;
        Name = name;
        Severity = severity;
        Message = message;
        Code = code;
        Source = source;
        OriginatedIn = source ?? "Unknown";
        Timestamp = DateTime.UtcNow;
        Details = null;
        Data = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageTemplate{TSeverity}"/> class with additional data.
    /// </summary>
    /// <param name="id">The unique identifier for this message type.</param>
    /// <param name="name">The name of this message type.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="code">The message code or identifier (optional).</param>
    /// <param name="source">The source component or operation (optional).</param>
    /// <param name="details">Additional details dictionary (optional).</param>
    /// <param name="data">Additional data object (optional).</param>
    protected MessageTemplate(int id, string name, TSeverity severity, string message, string? code = null, string? source = null, IDictionary<string, object?>? details = null, object? data = null)
    {
        Id = id;
        Name = name;
        Severity = severity;
        Message = message;
        Code = code;
        Source = source;
        OriginatedIn = source ?? "Unknown";
        Timestamp = DateTime.UtcNow;
        Details = details;
        Data = data;
    }

    /// <summary>
    /// Formats the message text with the provided parameters.
    /// </summary>
    /// <param name="args">The arguments to format into the message template.</param>
    /// <returns>A formatted message string.</returns>
    public virtual string Format(params object[] args)
    {
        if (args?.Length > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, Message, args);
        }
        return Message;
    }

    /// <summary>
    /// Creates a new instance of this message with a different severity level.
    /// </summary>
    /// <param name="severity">The new severity level.</param>
    /// <returns>A new message instance with the specified severity.</returns>
    public virtual MessageTemplate<TSeverity> WithSeverity(TSeverity severity)
    {
        // Default implementation - subclasses can override if they need different behavior
        throw new NotSupportedException($"WithSeverity is not supported for {GetType().Name}. Override this method if severity changes are needed.");
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current message.
    /// </summary>
    /// <param name="obj">The object to compare with the current message.</param>
    /// <returns>true if the specified object is equal to the current message; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not MessageTemplate<TSeverity> other)
            return false;

        return Id == other.Id &&
               string.Equals(Name, other.Name, StringComparison.Ordinal) &&
               typeof(TSeverity) == other.Severity.GetType() &&
               string.Equals(Severity.ToString(), other.Severity.ToString(), StringComparison.Ordinal) &&
               string.Equals(Message, other.Message, StringComparison.Ordinal) &&
               string.Equals(Code, other.Code, StringComparison.Ordinal) &&
               string.Equals(Source, other.Source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a hash code for the current message.
    /// </summary>
    /// <returns>A hash code for the current message.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Severity.ToString(), Code, Source);
    }

    /// <summary>
    /// Returns a string representation of the message.
    /// </summary>
    /// <returns>A string that represents the current message.</returns>
    public override string ToString()
    {
        var parts = new List<string> { $"[{Severity}]" };

        if (!string.IsNullOrEmpty(Code))
            parts.Add($"({Code})");

        parts.Add(Message);

        if (!string.IsNullOrEmpty(Source))
            parts.Add($"- Source: {Source}");

        parts.Add($"- {Timestamp:yyyy-MM-dd HH:mm:ss} UTC");

        return string.Join(" ", parts);
    }
}
