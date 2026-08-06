using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A live transaction scope on a single <see cref="IDataConnection"/>.
/// </summary>
/// <remarks>
/// Holds an open physical connection plus a started native transaction (SqlTransaction,
/// NpgsqlTransaction, etc.). All <c>Execute</c> calls on this scope run inside that
/// transaction. Commit or Rollback to end the scope.
/// <para>
/// The owner (DataGatewayTransaction) is responsible for calling Commit or Rollback and
/// then disposing. Dispose performs an implicit rollback if the transaction is still active,
/// so callers that propagate an exception without explicitly rolling back are still safe.
/// </para>
/// </remarks>
public interface IDataConnectionTransaction : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether this transaction is still active (not yet committed or rolled back).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Executes a data command inside this transaction against the unified container, returning a typed result.
    /// </summary>
    Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        IDataContainer container,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a data command inside this transaction against the unified container, returning a non-generic result.
    /// </summary>
    Task<IGenericResult> Execute(
        IDataCommand command,
        IDataContainer container,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits all operations performed within this transaction.
    /// </summary>
    Task<IGenericResult> Commit(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back all operations performed within this transaction.
    /// </summary>
    Task<IGenericResult> Rollback(CancellationToken cancellationToken = default);
}
