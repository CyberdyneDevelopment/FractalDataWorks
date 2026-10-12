using System;
using System.Collections.Generic;
using Fdw.Etl.Abstractions.Monitoring.HealthStateOptions;

namespace Fdw.Etl.Abstractions.Monitoring;

/// <summary>
/// Represents overall health status.
/// </summary>
public interface IHealthStatus
{
    /// <summary>
    /// Gets the overall status.
    /// </summary>
    IHealthState Status { get; }

    /// <summary>
    /// Gets individual health check results.
    /// </summary>
    IReadOnlyDictionary<string, IHealthCheckResult> Checks { get; }

    /// <summary>
    /// Gets the total duration of all health checks.
    /// </summary>
    TimeSpan TotalDuration { get; }
}