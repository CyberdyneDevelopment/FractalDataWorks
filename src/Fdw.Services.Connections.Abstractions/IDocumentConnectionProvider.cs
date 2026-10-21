using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>Supplies the document connections: the same connections as <see cref="IConnectionProvider"/>, those that store whole files (FDW-1496).</summary>
public interface IDocumentConnectionProvider
{
    /// <summary>Gets a document connection by name.</summary>
    /// <param name="name">The connection's name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connection, or the failure when none has that name or the one that has is not a document connection.</returns>
    Task<IGenericResult<IDocumentConnection>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a document connection by identity.</summary>
    /// <param name="id">The connection's identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The connection, or the failure.</returns>
    Task<IGenericResult<IDocumentConnection>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets every connection that stores documents.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document connections.</returns>
    Task<IGenericResult<IReadOnlyList<IDocumentConnection>>> Get(CancellationToken cancellationToken = default);
}
