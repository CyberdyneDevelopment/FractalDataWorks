using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Messages;
using Fdw.Services.Abstractions;

namespace Fdw.Services;

/// <summary>
/// Base class for all service-related messages.
/// Provides consistent message structure and formatting for service operations.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class ServiceMessageBase<T> : MessageTemplate<MessageSeverity>, IServiceMessage
    where T : ServiceMessageBase<T>
{

    // Properties inherited from MessageTemplate<MessageSeverity>:
    // - Severity (MessageSeverity)
    // - CurrentMessage (string)
    // - Code (string?)
    // - Source (string?)
    // - Timestamp (DateTime)
    // - Details (IDictionary<string, object?>?)
    // - Data (object?)

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceMessageBase{T}"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this message type.</param>
    /// <param name="name">The name of this message type.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="code">The message code or identifier (optional).</param>
    protected ServiceMessageBase(int id, string name, MessageSeverity severity,
                                string message, string? code = null)
        : base(id, name, severity, message, code, "Services", null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceMessageBase{T}"/> class with additional data.
    /// </summary>
    /// <param name="id">The unique identifier for this message type.</param>
    /// <param name="name">The name of this message type.</param>
    /// <param name="severity">The severity level of the message.</param>
    /// <param name="message">The human-readable message text.</param>
    /// <param name="code">The message code or identifier (optional).</param>
    /// <param name="details">Additional details dictionary (optional).</param>
    /// <param name="data">Additional data object (optional).</param>
    protected ServiceMessageBase(int id, string name, MessageSeverity severity,
                                string message, string? code = null,
                                IDictionary<string, object?>? details = null, object? data = null)
        : base(id, name, severity, message, code, "Services", details, data)
    {
    }

    /// <summary>
    /// Formats the message text with the provided parameters.
    /// </summary>
    /// <param name="args">The arguments to format into the message template.</param>
    /// <returns>A formatted message string.</returns>
    public override string Format(params object[] args)
    {
        if (args?.Length > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, Message, args);
        }
        return Message;
    }
}