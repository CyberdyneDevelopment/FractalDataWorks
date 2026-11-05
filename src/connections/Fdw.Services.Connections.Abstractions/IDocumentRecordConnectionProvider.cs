using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opens a connection as a DocumentRecord connection (FDW-3673): itself when it is one, otherwise through the DocumentRecord extension that
/// takes its kind (<see cref="DocumentRecords.DocumentRecordConnections"/>); a connection neither is refused.
/// </summary>
public interface IDocumentRecordConnectionProvider
{
    /// <summary>Opens a connection by name as a DocumentRecord connection.</summary>
    /// <param name="name">The connection's name.</param>
    /// <param name="cancellationToken">Cancels the open.</param>
    /// <returns>The connection, or the reason it is not one.</returns>
    Task<IGenericResult<IDocumentRecordConnection>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Opens a connection by id as a DocumentRecord connection.</summary>
    /// <param name="id">The connection's id.</param>
    /// <param name="cancellationToken">Cancels the open.</param>
    /// <returns>The connection, or the reason it is not one.</returns>
    Task<IGenericResult<IDocumentRecordConnection>> Get(Guid id, CancellationToken cancellationToken = default);
}
