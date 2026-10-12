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
