using System;
using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// Represents a single log entry.
/// </summary>
public interface ILogEntry
{
    /// <summary>
    /// Gets the log entry timestamp.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Gets the log level.
    /// </summary>
    ILogLevel Level { get; }

    /// <summary>
    /// Gets the log message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the source/category of the log entry.
    /// </summary>
    string? Source { get; }

    /// <summary>
    /// Gets the exception details if this is an error log.
    /// </summary>
    string? Exception { get; }

    /// <summary>
    /// Gets additional properties/metadata for the log entry.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Properties { get; }

    /// <summary>
    /// Gets the correlation ID for tracing related log entries.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the span ID for distributed tracing.
    /// </summary>
    string? SpanId { get; }
}