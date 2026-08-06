// Based on Microsoft.Extensions.Logging.LoggerMessageAttribute
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Extended by Fdw to support IGenericMessage return types

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Messages;

namespace Fdw.MessageLogging;

/// <summary>
/// Provides information to guide the production of a strongly typed logging method that returns an IGenericMessage.
/// </summary>
/// <remarks>
/// <para>The method this attribute is applied to:</para>
/// <para>   - Must be a partial method.</para>
/// <para>   - Must return <see cref="IGenericMessage"/> or a type that implements it.</para>
/// <para>   - Must not be generic.</para>
/// <para>   - Must have an <see cref="ILogger"/> as one of its parameters.</para>
/// <para>   - None of the parameters can be generic.</para>
/// </remarks>
// Why: pure attribute definition (declarative metadata only, consumed by the MessageLogging source generator) — no logic to unit test.
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class MessageLoggingAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageLoggingAttribute"/> class.
    /// </summary>
    public MessageLoggingAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageLoggingAttribute"/> class
    /// with the specified event ID, log level, and message.
    /// </summary>
    /// <param name="eventId">The event ID for the log message.</param>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message template.</param>
    public MessageLoggingAttribute(int eventId, LogLevel level, string message)
    {
        EventId = eventId;
        Level = level;
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageLoggingAttribute"/> class
    /// with the specified log level and message.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message template.</param>
    public MessageLoggingAttribute(LogLevel level, string message)
    {
        Level = level;
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageLoggingAttribute"/> class
    /// with the specified log level.
    /// </summary>
    /// <param name="level">The log level.</param>
    public MessageLoggingAttribute(LogLevel level)
    {
        Level = level;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageLoggingAttribute"/> class
    /// with the specified message.
    /// </summary>
    /// <param name="message">The message template.</param>
    public MessageLoggingAttribute(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Gets or sets the event ID for the log message.
    /// </summary>
    public int EventId { get; set; } = -1;

    /// <summary>
    /// Gets or sets the event name for the log message.
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the log level. This is used for logging purposes.
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.None;

    /// <summary>
    /// Gets or sets the message template.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to skip the enabled check.
    /// </summary>
    public bool SkipEnabledCheck { get; set; }

    /// <summary>
    /// Gets or sets the message severity for the returned IGenericMessage.
    /// This is separate from LogLevel and is used in the Fdw.Messages infrastructure.
    /// If not specified, it will be mapped from the LogLevel.
    /// </summary>
    public MessageSeverity Severity { get; set; } = MessageSeverity.Information;

    /// <summary>
    /// Gets or sets a value indicating whether the severity should be automatically mapped from LogLevel.
    /// When true (default), the Severity property is ignored and mapped from Level instead.
    /// </summary>
    public bool AutoMapSeverity { get; set; } = true;

    /// <summary>
    /// Gets or sets a type code prefix for the generated message code.
    /// The generated code will be "{TypeCode}-{EventId}" (e.g., "FDW-8001").
    /// Must be 2-6 uppercase alphanumeric characters.
    /// Defaults to { 'F', 'D', 'W' } when not specified.
    /// </summary>
    public char[]? TypeCode { get; set; }
}
