using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Service that routes data commands to the appropriate connection.
/// Addressing (DataStore, Path, Container) is supplied via <see cref="DataStoreTarget"/> or
/// <see cref="DataSetTarget"/> — never on the command itself.
/// </summary>
public interface IDataGateway
{
    /// <summary>
    /// Executes a data command against an explicitly identified container within a DataStore.
    /// The gateway resolves the physical connection from the DataStore's registered ConnectionId.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="command">The data command to execute.</param>
    /// <param name="target">
    /// The DataStore/Path/Container address. <see cref="DataStoreTarget.Path"/> may be
    /// <see langword="null"/> to search all paths in the store.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops every cached result for the rows a target addresses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A write executed through this gateway already invalidates its own container, so ordinary
    /// callers never need this. It exists for the one case the gateway cannot observe: a write that
    /// ran inside a transaction. Those rows are not visible to anyone until the caller commits, so
    /// invalidating at execute time evicts nothing and the first read after the commit is served the
    /// pre-transaction rows. The committer calls this once, after Commit succeeds.
    /// </para>
    /// <para>
    /// It never fails. Invalidation runs after a write has already been persisted, so reporting a
    /// problem here could only ask a caller to undo work that succeeded.
    /// </para>
    /// </remarks>
    /// <param name="target">The DataStore/Path/Container whose cached results are now stale.</param>
    void InvalidateCachedResults(DataStoreTarget target);

    /// <summary>
    /// Executes a data command against an explicitly identified container within a DataStore,
    /// with explicit control over whether the result cache is consulted on this call.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="command">The data command to execute.</param>
    /// <param name="target">The DataStore/Path/Container address.</param>
    /// <param name="useCache">
    /// When <see langword="true"/> (default), a cached result is returned if one exists.
    /// When <see langword="false"/>, the cache read is skipped and a fresh result is fetched;
    /// the fresh result is still written to the cache (force-refresh). Has no effect when
    /// <c>DataGatewayOptions.EnableCache</c> is <see langword="false"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataStoreTarget target, bool useCache, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a data command through the DataSet federation layer identified by name.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="command">The data command to execute.</param>
    /// <param name="target">The DataSet name to route through.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution result.</returns>
    Task<IGenericResult<T>> Execute<T>(IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a streaming, low-allocation record-source cursor over a read command against a container
    /// in a DataStore, instead of materializing the whole result set.
    /// </summary>
    /// <remarks>
    /// The gateway resolves the physical connection from the DataStore's registered ConnectionId and,
    /// when that connection supports <c>IRecordSourceConnection</c>, returns a cursor whose
    /// <see cref="DataRecord"/> rows expose their values as a <c>ReadOnlySpan&lt;object?&gt;</c> over a
    /// shared schema flyweight (no per-row dictionary/key allocation). The returned
    /// <see cref="IRecordSource{T}"/> OWNS the underlying reader and connection — the caller MUST dispose
    /// it (use <c>await using</c>). Connections that cannot stream a cursor fail loud with a structured
    /// non-success result; the caller falls back to the materializing <see cref="Execute{T}(IDataCommand, DataStoreTarget, CancellationToken)"/> path.
    /// </remarks>
    /// <param name="command">The read command (e.g. a <c>QueryCommand</c>).</param>
    /// <param name="target">The DataStore/Path/Container address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>On success, a disposable record-source cursor; on failure, a structured non-success result.</returns>
    Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a transaction scope on the named connection.
    /// </summary>
    /// <remarks>
    /// All <see cref="IDataGatewayTransaction.Execute{T}"/> calls on the returned scope run
    /// on the same physical connection inside the same native database transaction.
    /// Use <see cref="IDataGatewayTransaction.Commit"/> to persist or
    /// <see cref="IDataGatewayTransaction.Rollback"/> to discard. Disposing without committing
    /// performs an implicit rollback.
    /// <para>
    /// Returns a failure result when the named connection does not support transactions
    /// (e.g., REST connections), when the connection cannot be opened, or when the underlying
    /// driver refuses to start the transaction. On failure nothing is opened and the caller
    /// does not need to dispose anything.
    /// </para>
    /// </remarks>
    /// <param name="connectionName">The name of the connection on which to open the transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A result containing the open transaction scope on success, or a structured failure.
    /// Dispose the scope when done (use <c>await using</c>).
    /// </returns>
    Task<IGenericResult<IDataGatewayTransaction>> BeginTransaction(
        string connectionName,
        CancellationToken cancellationToken = default);
}
