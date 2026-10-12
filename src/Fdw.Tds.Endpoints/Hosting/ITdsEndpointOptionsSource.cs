using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// Supplies the options a SQL Server endpoint listens with, read when the listener starts.
/// </summary>
/// <remarks>
/// The options are read once the container exists rather than handed in ready-made, because they include
/// the TLS certificate, which comes from a secret manager and can only be read asynchronously. A source that
/// cannot supply them fails, and the listener does not start.
/// </remarks>
public interface ITdsEndpointOptionsSource
{
    /// <summary>Reads the options.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The options, or why they cannot be read.</returns>
    Task<IGenericResult<TdsEndpointOptions>> Read(CancellationToken cancellationToken = default);
}
