using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types that support a lightweight health probe.
/// </summary>
/// <remarks>
/// Connection types implement this interface to expose a cheap, bounded liveness check of the
/// underlying backend (e.g. <c>SELECT 1</c>, an HTTP HEAD request, a path existence check).
/// Consistent with the <c>ISupportsContainerTypes</c> capability pattern. A connection whose type
/// does NOT implement this interface is reported by the health-check domain as unprobed rather
/// than assumed healthy.
/// </remarks>
public interface ISupportsHealthProbe
{
    /// <summary>
    /// Performs a cheap, bounded liveness probe of the underlying backend
    /// (e.g. <c>SELECT 1</c>, a HEAD request, a path existence check).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the backend responded successfully.</returns>
    Task<IGenericResult> Probe(CancellationToken cancellationToken = default);
}
