using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Web.RestEndpoints.Caching;

/// <summary>
/// Provides ETag generation for conditional GET support on CRUD endpoints.
/// Implementations determine the ETag value based on data state (e.g., RowId versioning).
/// </summary>
public interface IETagProvider
{
    /// <summary>
    /// Generates an ETag for the specified container and connection.
    /// Returns null if an ETag cannot be computed (e.g., container has no versioning column).
    /// </summary>
    /// <param name="containerName">The data container name (table, collection, etc.).</param>
    /// <param name="connectionName">The connection name used for data access routing.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A quoted ETag string (e.g., <c>"a1b2c3d4"</c>) per HTTP spec, or null if unavailable.
    /// </returns>
    Task<string?> GetETag(string containerName, string connectionName, CancellationToken ct);
}
