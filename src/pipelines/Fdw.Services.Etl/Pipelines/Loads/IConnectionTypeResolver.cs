using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Finds the connection type of a connection, so a load can be checked against what that type declares it can run (FDW-1109).
/// </summary>
public interface IConnectionTypeResolver
{
    /// <summary>Finds the type of a connection.</summary>
    /// <param name="connectionId">The connection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connection type, or a failure saying why it cannot be found.</returns>
    Task<IGenericResult<IConnectionType>> ForConnection(Guid connectionId, CancellationToken cancellationToken = default);
}
