using System;
using System.Collections.Generic;
using Fdw.Etl.Abstractions.Monitoring.HealthStateOptions;

namespace Fdw.Etl.Abstractions.Monitoring;

/// <summary>
/// Represents the result of a health check.
/// </summary>
public interface IHealthCheckResult
{
    /// <summary>
    /// Gets the health check status.
    /// </summary>
    IHealthState Status { get; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets any exception that occurred.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Gets additional data.
    /// </summary>
    IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// Gets the duration of the check.
    /// </summary>
    TimeSpan Duration { get; }
}