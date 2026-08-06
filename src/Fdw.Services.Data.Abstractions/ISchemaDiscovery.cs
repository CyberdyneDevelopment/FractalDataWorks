using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Non-generic schema discovery interface implemented by connection type options.
/// Allows the orchestrator to call DiscoverSchema without knowing the concrete connection type.
/// </summary>
public interface ISchemaDiscovery
{
    /// <summary>
    /// Discovers the schema containers using the provided connection.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        IGenericConnection connection,
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
    /// Discovers the schema using a typed connection.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        TConnection connection,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken);
}
