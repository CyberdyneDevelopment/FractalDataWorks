using System;
using System.Collections.Generic;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a log entry.
/// </summary>
public sealed class LogEntry : ILogEntry
{
    private Dictionary<string, object?>? _properties;

    /// <inheritdoc />
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public ILogLevel Level { get; set; } = LogLevels.Information;

    /// <inheritdoc />
    public string Message { get; set; } = "";

    /// <inheritdoc />
    public string? Source { get; set; }

    /// <inheritdoc />
    public string? Exception { get; set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?>? Properties => _properties;

    /// <inheritdoc />
    public string? CorrelationId { get; set; }

    /// <inheritdoc />
    public string? SpanId { get; set; }

    /// <summary>
    /// Sets a property value.
    /// </summary>
    public void SetProperty(string key, object? value)
    {
        _properties ??= new Dictionary<string, object?>(StringComparer.Ordinal);
        _properties[key] = value;
    }

    /// <summary>
    /// Creates a trace log entry.
    /// </summary>
    public static LogEntry Trace(string message, string? source = null) =>
        new() { Level = LogLevels.Trace, Message = message, Source = source };

    /// <summary>
    /// Creates a debug log entry.
    /// </summary>
    public static LogEntry Debug(string message, string? source = null) =>
        new() { Level = LogLevels.Debug, Message = message, Source = source };

    /// <summary>
    /// Creates an information log entry.
    /// </summary>
    public static LogEntry Info(string message, string? source = null) =>
        new() { Level = LogLevels.Information, Message = message, Source = source };

    /// <summary>
    /// Creates a warning log entry.
    /// </summary>
    public static LogEntry Warn(string message, string? source = null) =>
        new() { Level = LogLevels.Warning, Message = message, Source = source };

    /// <summary>
    /// Creates an error log entry.
    /// </summary>
    public static LogEntry Error(string message, string? exception = null, string? source = null) =>
        new() { Level = LogLevels.Error, Message = message, Exception = exception, Source = source };

    /// <summary>
    /// Creates a critical log entry.
    /// </summary>
    public static LogEntry Critical(string message, string? exception = null, string? source = null) =>
        new() { Level = LogLevels.Critical, Message = message, Exception = exception, Source = source };

    /// <summary>
    /// Gets the level abbreviation for display.
    /// </summary>
    public string GetLevelAbbreviation() => Level.Name switch
    {
        "Trace" => "TRC",
        "Debug" => "DBG",
        "Information" => "INF",
        "Warning" => "WRN",
        "Error" => "ERR",
        "Critical" => "CRT",
        _ => "???"
    };
}
