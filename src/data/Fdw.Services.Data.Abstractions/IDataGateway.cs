using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

using Fdw.Abstractions;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Service that routes data commands to the appropriate connection.
/// Addressing (DataStore, Path, Container) is supplied via <see cref="DataStoreTarget"/> or
/// <see cref="DataSetTarget"/> — never on the command itself.
/// </summary>
public interface IDataGateway : IGenericService
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
    /// the fresh result is still written to the cache (force-refresh).
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
    /// Opens a container's rows as a stream read in a stable order, so no row is repeated or skipped however many the container holds
    /// (FDW-4911).
    /// </summary>
    /// <remarks>
    /// The gateway resolves the container and its store's connection. A connection that implements <c>IRecordSourceConnection</c> is read
    /// through its cursor, one statement whose records are cut to the fields the same query returns through <c>Execute</c>. A connection
    /// that states its store keeps its own order (<c>IStoreOrderedConnection</c>: a remote API paged by its protocol, a file) is paged in
    /// that order, the store's guarantee. Any other connection is read in keyset pages: every field of the container's primary key,
    /// ascending, each page asking for the rows after the last key, <c>DataGatewayServiceTypes.StableReadPageSize</c> rows at a time. A container that can be read neither way is refused with
    /// <c>StableReadUnavailable</c>, naming it and what it needs; rows are never paged in no stated order and no key is guessed. The command's
    /// own ordering and paging are not used by a keyset read; its filter, projection and field selection apply to every page. The result
    /// cache is never read or written.
    /// <para>
    /// The returned <see cref="IRecordSource{T}"/> OWNS what it reads through -- the caller MUST dispose it (use <c>await using</c>). The
    /// connection's outbound limits apply exactly as they do to <c>Execute</c>: rate and result-size checks and the daily budget are enforced
    /// before the read opens. The other two last as long as the read does: it occupies one of the connection's concurrency slots until it is
    /// disposed, so dispose it promptly, and the query timeout bounds its whole lifetime -- enumeration ends with a timeout failure once it fires.
    /// </para>
    /// </remarks>
    /// <param name="command">The read: a query command.</param>
    /// <param name="target">The DataStore/Path/Container address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>On success, a disposable record source; on failure, a structured non-success result.</returns>
    Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, DataStoreTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a data set's rows as a stream read in a stable order, so no row is repeated or skipped (FDW-4911).
    /// </summary>
    /// <remarks>
    /// A data set whose kind pages in the store is read in keyset pages by its key fields, <c>DataGatewayServiceTypes.StableReadPageSize</c> rows
    /// at a time; one that declares no key is refused with <c>DataSetNotPageable</c>. A kind that does not page in the store (an in-memory
    /// join) builds its whole set to answer any read, so it is read once, unpaged, and refused with <c>UnpagedDataSetOverRowCap</c> when it
    /// holds more rows than <c>DataGatewayServiceTypes.UnpagedDataSetRowCap</c>. The data set is resolved and admitted as for
    /// <see cref="Execute{T}(IDataCommand, DataSetTarget, CancellationToken)"/>; each record carries the data set's declared fields. The caller
    /// MUST dispose the returned source.
    /// </remarks>
    /// <param name="command">The read: a query command over the data set's field names.</param>
    /// <param name="target">The data set to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>On success, a disposable record source; on failure, a structured non-success result.</returns>
    Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, DataSetTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a vectorized command (the rows nearest a vector, say) against a container whose store holds vectors.
    /// </summary>
    /// <remarks>
    /// Only a connection implementing <c>IVectorizedDataConnection</c> takes it: a store whose connection does not is
    /// refused, as <see cref="BeginTransaction"/> refuses a connection that cannot hold a transaction. A store the host
    /// declares is reached as for a data command, through its configuration gateway and by the system context only. The
    /// connection's limits apply as they do to a data command; the answer is never cached (FDW-1354).
    /// </remarks>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="command">The vectorized command.</param>
    /// <param name="target">The DataStore/Path/Container address for this command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The command's answer, or why it was refused or failed.</returns>
    Task<IGenericResult<T>> Execute<T>(IVectorizedDataCommand<T> command, DataStoreTarget target, CancellationToken cancellationToken = default);

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
