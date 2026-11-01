using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Service for monitoring system and service health, including throughput and history.
/// </summary>
/// <remarks>
/// Extends <see cref="IGenericService"/> so the health monitor is a full FDW service-domain member:
/// implementations are created by <see cref="IHealthMonitorFactory{TService,TConfiguration}"/> options
/// and resolved through the domain provider — never registered directly against this interface (a
/// direct registration is exactly the registration-order race the domain exists to eliminate). The
/// domain is query-only: implementations fail loud (structured failure) on <c>Execute</c>.
/// </remarks>
public interface IHealthMonitorService : IServiceOption
{
    /// <summary>
    /// Gets the current health snapshot for the entire system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the system health snapshot.</returns>
    Task<IGenericResult<SystemHealthSnapshot>> GetSystemHealth(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health snapshot for a specific service.
    /// </summary>
    /// <param name="serviceName">The name of the service to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the service health snapshot.</returns>
    Task<IGenericResult<ServiceHealthSnapshot>> GetServiceHealth(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets throughput data for a service over a specified time window.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="window">The time window to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the throughput data.</returns>
    Task<IGenericResult<ThroughputData>> GetThroughput(string serviceName, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets health check history for a service over a specified time window.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <param name="window">The time window to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the health check history.</returns>
    Task<IGenericResult<IReadOnlyList<HealthCheckPoint>>> GetHealthHistory(string serviceName, TimeSpan window, CancellationToken cancellationToken = default);
}
