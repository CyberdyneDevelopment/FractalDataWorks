using System;
using System.Collections.Generic;
using Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

namespace Fdw.Etl.Abstractions.Monitoring;

/// <summary>
/// Service for sending telemetry events.
/// </summary>
public interface IEtlTelemetryService
{
    /// <summary>
    /// Tracks a custom event.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="properties">Event properties.</param>
    /// <param name="metrics">Event metrics.</param>
    void TrackEvent(
        string eventName,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null);

    /// <summary>
    /// Tracks a dependency call (SQL, HTTP, etc.).
    /// </summary>
    /// <param name="dependencyType">The dependency type (SQL, HTTP, etc.).</param>
    /// <param name="dependencyName">The dependency name.</param>
    /// <param name="data">The command data.</param>
    /// <param name="startTime">The start time.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="success">Whether the call succeeded.</param>
    /// <param name="properties">Additional properties.</param>
    void TrackDependency(
        string dependencyType,
        string dependencyName,
        string data,
        DateTimeOffset startTime,
        TimeSpan duration,
        bool success,
        IDictionary<string, string>? properties = null);

    /// <summary>
    /// Tracks an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="properties">Additional properties.</param>
    /// <param name="metrics">Additional metrics.</param>
    void TrackException(
        Exception exception,
        IDictionary<string, string>? properties = null,
        IDictionary<string, double>? metrics = null);

    /// <summary>
    /// Tracks a trace message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="severityLevel">The severity level.</param>
    /// <param name="properties">Additional properties.</param>
    void TrackTrace(
        string message,
        ISeverityLevel severityLevel,
        IDictionary<string, string>? properties = null);

    /// <summary>
    /// Flushes the telemetry buffer.
    /// </summary>
    void Flush();
}

// SeverityLevel enum replaced by SeverityLevels TypeCollection
// See Fdw.Etl.Monitoring.Abstractions.SeverityLevels namespace
