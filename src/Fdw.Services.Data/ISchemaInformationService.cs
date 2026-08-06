using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// Provides on-demand access to the discovered schema for a named connection.
/// Callers do not need to know whether the schema is already cached or needs to be discovered.
/// </summary>
/// <remarks>
/// <para>
/// This service is the primary entry point for getting DataStore metadata at runtime.
/// It replaces the startup-only <c>SchemaDiscoveryStartupService</c> pattern with a
/// demand-driven API that endpoints and background services can call directly.
/// </para>
/// <para>
/// <see cref="GetSchema"/> returns cached metadata when available; otherwise it discovers,
/// persists, and returns it. <see cref="RefreshSchema"/> always re-discovers, enabling the
/// "Re-discover" button in the Management UI.
/// </para>
/// <para>
/// Whether discovery is attempted at all is read from the connection's
/// <see cref="Fdw.Services.Connections.ConnectionConfiguration.DiscoveryEnabled"/>.
/// Discovery *scope* (which db/schema/table is visible) is expressed by DataStore/DataPath/DataContainer
/// records and gated by RBAC — Connection carries no schema include/exclude lists.
/// </para>
/// </remarks>
public interface ISchemaInformationService
{
    /// <summary>
    /// Gets schema information for the named connection.
    /// Returns cached DataStore metadata when available; discovers and persists it when not.
    /// </summary>
    /// <param name="connectionName">The name of the connection to get schema for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A success result containing <see cref="SchemaInformation"/> if discovery is enabled and succeeds,
    /// or a failure result if discovery is disabled or the connection cannot be resolved.
    /// </returns>
    Task<IGenericResult<SchemaInformation>> GetSchema(
        string connectionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces re-discovery of schema for the named connection, persists the results, and returns updated metadata.
    /// </summary>
    /// <param name="connectionName">The name of the connection to re-discover.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A success result containing <see cref="SchemaInformation"/> if discovery is enabled and succeeds,
    /// or a failure result if discovery is disabled or the connection cannot be resolved.
    /// </returns>
    Task<IGenericResult<SchemaInformation>> RefreshSchema(
        string connectionName,
        CancellationToken cancellationToken = default);
}
