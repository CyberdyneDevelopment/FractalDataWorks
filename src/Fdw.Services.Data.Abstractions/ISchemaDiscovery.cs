using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Performs schema discovery for a connection kind. Implemented only by connection types that declare
/// at least one discovering option in <see cref="IConnectionType.SupportedDiscoveryTypes"/>.
/// Allows the orchestrator to call DiscoverSchema without knowing the concrete connection type.
/// </summary>
/// <remarks>
/// Callers never test for this interface to decide whether a connection discovers; they read the
/// connection's chosen option (<see cref="IConnectionImplementationConfiguration.SchemaDiscovery"/>)
/// against the kind's declared options. A kind that declares a discovering option and does not
/// implement this is a registration defect, and discovery fails loud on it.
/// </remarks>
public interface ISchemaDiscovery
{
    /// <summary>
    /// Discovers the schema containers using the provided connection and the discovery option the
    /// connection chose.
    /// </summary>
    /// <param name="connection">The open connection to discover through.</param>
    /// <param name="discoveryType">
    /// The connection's chosen option. Always one of this kind's declared options and always one that
    /// discovers; a kind with several strategies runs the one named here.
    /// </param>
    /// <param name="options">Which schemas and object kinds to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        IGenericConnection connection,
        ISchemaDiscoveryType discoveryType,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken);
}

/// <summary>
/// Generic variant of <see cref="ISchemaDiscovery"/> for typed connection access.
/// Implemented by concrete connection type options (e.g., MsSqlConnectionType).
/// </summary>
/// <typeparam name="TConnection">The concrete connection type.</typeparam>
public interface ISchemaDiscovery<in TConnection> : ISchemaDiscovery
    where TConnection : IGenericConnection
{
    /// <summary>
    /// Discovers the schema using a typed connection and the discovery option the connection chose.
    /// </summary>
    /// <param name="connection">The open, typed connection to discover through.</param>
    /// <param name="discoveryType">The connection's chosen option; see <see cref="ISchemaDiscovery"/>.</param>
    /// <param name="options">Which schemas and object kinds to include.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        TConnection connection,
        ISchemaDiscoveryType discoveryType,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken);
}
