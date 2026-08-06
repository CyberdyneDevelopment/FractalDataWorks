using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Optional connection capability: open a streaming, low-allocation record-source cursor over a query
/// result instead of materializing the whole result set.
/// </summary>
/// <remarks>
/// <para>
/// Why a separate capability interface (not a member of <c>IDataConnection</c>): the abstraction
/// targets <c>netstandard2.0</c>, which has no default interface methods, so adding this to
/// <c>IDataConnection</c> would force EVERY connection type to implement it. Connections that can read
/// through a cursor (e.g. MsSql via <c>DataReaderRowSource</c>) implement this; the DataGateway
/// feature-detects it and falls back to the materializing <c>Execute</c> path for connections that do not.
/// </para>
/// <para>
/// The returned <see cref="IRecordSource{T}"/> OWNS the underlying reader and connection. It is
/// <see cref="System.IDisposable"/>/<see cref="System.IAsyncDisposable"/> — the caller MUST dispose it
/// to release the held connection. Each <see cref="DataRecord"/> exposes its values as a
/// <c>ReadOnlySpan&lt;object?&gt;</c> over a shared schema flyweight (no per-row dictionary/key allocation).
/// </para>
/// </remarks>
public interface IRecordSourceConnection
{
    /// <summary>
    /// Opens a streaming record-source cursor over the result of the given query command.
    /// </summary>
    /// <param name="command">The query command (must be a read; writes are not valid here).</param>
    /// <param name="container">The container whose schema/location the command targets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// On success, an <see cref="IRecordSource{T}"/> the caller must dispose; on failure, a non-success result.
    /// </returns>
    Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default);
}
