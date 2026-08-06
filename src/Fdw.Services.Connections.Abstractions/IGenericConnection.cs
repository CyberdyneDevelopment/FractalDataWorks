using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Interface for Fdw framework connections.
/// Provides a framework-specific interface for connection implementations.
/// </summary>
public interface IGenericConnection : IDisposable, IServiceOption
{
    /// <summary>
    /// Gets a value indicating whether this connection is stale and should be recreated.
    /// A connection is stale when it has been disposed or its underlying resources have been released.
    /// </summary>
    bool IsStale { get; }

    /// <summary>
    /// Tests connectivity to the underlying resource.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the connection test succeeded.</returns>
    Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default);
}