using System;
using System.Collections.Generic;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Result of a service health check operation.
/// </summary>
/// <remarks>
/// Provides structured information about service health including status,
/// diagnostic data, and timing information.
/// </remarks>
public interface IHealthCheckResult
{
    /// <summary>
    /// Gets the health status of the service.
    /// </summary>
    /// <value>The health state indicating whether the service is Healthy, Unhealthy, or Degraded.</value>
    IHealthState Status { get; }

    /// <summary>
    /// Gets a human-readable description of the health check result.
    /// </summary>
    /// <value>Optional description providing context about the health status.</value>
    string? Description { get; }

    /// <summary>
    /// Gets the exception that occurred during the health check, if any.
    /// </summary>
    /// <value>The exception that caused the health check to fail, or null if no exception occurred.</value>
    Exception? Exception { get; }

    /// <summary>
    /// Gets additional diagnostic data collected during the health check.
    /// </summary>
    /// <value>Dictionary of diagnostic information such as version numbers, connection states, etc.</value>
    IReadOnlyDictionary<string, object> Data { get; }

    /// <summary>
    /// Gets the duration of the health check operation.
    /// </summary>
    /// <value>Time taken to complete the health check.</value>
    TimeSpan Duration { get; }
}
