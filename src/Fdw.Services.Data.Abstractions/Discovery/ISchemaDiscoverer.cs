using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Abstractions.Discovery;

/// <summary>
/// Connection-type-agnostic schema discovery — given a connected
/// <see cref="IGenericConnection"/>, list the tables/views (and optionally their
/// columns) the underlying store exposes.
/// </summary>
// Why: typed discoverers (IMsSqlSchemaDiscoverer, IPostgreSqlSchemaDiscoverer) have
// fundamentally similar shape but take their own connection types. ISchemaDiscoverer
// is the cross-cutting abstraction the CLI / web UI talks to; it dispatches via
// ISchemaDiscoveryFactory to the right typed implementation per registered connection
// type. Returns a flat list of containers — full ManagedConfiguration shape can be
// mapped from this by the caller (e.g., ConfigurationGatewayDataStoreProvider Save).
public interface ISchemaDiscoverer
{
    /// <summary>
    /// Discovers the containers (tables/views/endpoints) reachable through the
    /// supplied connection. Implementations should not throw — failures roll up
    /// as <see cref="IGenericResult{T}"/> failures.
    /// </summary>
    Task<IGenericResult<IReadOnlyList<IDiscoveredContainer>>> DiscoverContainers(
        IGenericConnection connection,
        CancellationToken cancellationToken = default);
}
