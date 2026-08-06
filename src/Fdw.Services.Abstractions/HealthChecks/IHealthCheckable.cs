using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Abstractions.Health;

/// <summary>
/// Interface for services that support health checking.
/// </summary>
/// <remarks>
/// Services that implement this interface can report their health status,
/// enabling monitoring, alerting, and diagnostics.
/// </remarks>
public interface IHealthCheckable
{
    /// <summary>
    /// Gets the display name for this health-checkable service.
    /// Used for lookup and monitoring identification instead of reflection.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Performs a health check for this service.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the health check status.</returns>
    /// <remarks>
    /// Health checks should verify:
    /// <list type="bullet">
    /// <item><description>Service availability and responsiveness</description></item>
    /// <item><description>Dependency resolution</description></item>
    /// <item><description>Configuration validity</description></item>
    /// <item><description>External resource connectivity (databases, APIs, etc.)</description></item>
    /// </list>
    /// </remarks>
    Task<IGenericResult<IHealthCheckResult>> CheckHealth(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default);
}
