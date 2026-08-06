using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// A transaction scope opened by <see cref="IDataGateway.BeginTransaction"/>.
/// </summary>
/// <remarks>
/// <para>
/// All <c>Execute</c> calls on this scope run on the same physical connection inside the
/// same native database transaction (SqlTransaction, NpgsqlTransaction, etc.).
/// </para>
/// <para>
/// Typical usage — authz change + security stamp bump:
/// <code>
/// await using var txn = await _gateway.BeginTransaction("AuthDb", ct);
/// if (!txn.IsSuccess) return ...; // fail clean — nothing was written
/// var scope = txn.Value!;
///
/// var changeResult = await scope.Execute&lt;int&gt;(changeCommand, changeTarget, ct);
/// if (!changeResult.IsSuccess) { await scope.Rollback(ct); return ...; }
///
/// var stampResult = await scope.Execute&lt;int&gt;(stampCommand, stampTarget, ct);
/// if (!stampResult.IsSuccess) { await scope.Rollback(ct); return ...; }
///
/// await scope.Commit(ct);
/// </code>
/// </para>
/// <para>
/// ETL multi-write usage:
/// <code>
/// await using var txn = await _gateway.BeginTransaction("OpsDb", ct);
/// var scope = txn.Value!;
///
/// await scope.Execute&lt;int&gt;(insertExecutionCommand, executionTarget, ct);
/// await scope.Execute&lt;int&gt;(insertOutputCommand, outputTarget, ct);
/// await scope.Execute&lt;int&gt;(updateStatusCommand, statusTarget, ct);
///
/// await scope.Commit(ct);
/// </code>
/// </para>
/// <para>
/// Dispose always performs an implicit rollback if the transaction has not been committed,
/// so exception paths that skip explicit Rollback are still safe.
/// </para>
/// </remarks>
public interface IDataGatewayTransaction : System.IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether this transaction is still active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Executes a data command inside this transaction, returning a typed result.
    /// The <paramref name="target"/> must address a container on the same connection this
    /// transaction was opened on.
    /// </summary>
    Task<IGenericResult<T>> Execute<T>(
        IDataCommand command,
        DataStoreTarget target,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a data command inside this transaction, returning a non-generic result.
    /// The <paramref name="target"/> must address a container on the same connection this
    /// transaction was opened on.
    /// </summary>
    Task<IGenericResult> Execute(
        IDataCommand command,
        DataStoreTarget target,
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
