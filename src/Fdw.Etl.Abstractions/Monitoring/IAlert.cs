using System;
using System.Collections.Generic;
using Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;

namespace Fdw.Etl.Abstractions.Monitoring;

/// <summary>
/// Represents an alert to be sent.
/// </summary>
public interface IAlert
{
    /// <summary>
    /// Gets the alert ID.
    /// </summary>
    string AlertId { get; }

    /// <summary>
    /// Gets the alert severity.
    /// </summary>
    IAlertSeverity Severity { get; }

    /// <summary>
    /// Gets the alert title.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the alert message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the source of the alert (pipeline, stage, etc.).
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Gets when the alert was triggered.
    /// </summary>
    DateTimeOffset TriggeredAt { get; }

    /// <summary>
    /// Gets additional details.
    /// </summary>
    IReadOnlyDictionary<string, object> Details { get; }

    /// <summary>
    /// Gets the channels to send this alert to.
    /// </summary>
    IReadOnlyList<string> Channels { get; }
}