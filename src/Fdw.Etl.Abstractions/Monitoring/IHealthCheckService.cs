using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Etl.Abstractions.Monitoring;

/// <summary>
/// Service for performing health checks.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Performs a health check.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing health status.</returns>
    Task<IGenericResult<IHealthStatus>> CheckHealth(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a health check.
    /// </summary>
    /// <param name="name">The health check name.</param>
    /// <param name="check">The health check function.</param>
    void RegisterHealthCheck(string name, Func<CancellationToken, Task<IHealthCheckResult>> check);
}


