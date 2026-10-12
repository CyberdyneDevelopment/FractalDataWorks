using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Logging;
using Fdw.Web.RestEndpoints.Models;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Every row a data query keeps, read batch after batch through <see cref="DataSetQuery"/> as the response is written
/// (FDW-1209).
/// </summary>
/// <remarks>
/// <para>
/// For a caller no page limit applies to. The response does not hold the rows: each batch is read when the one before has been
/// written, so memory holds one batch whatever the data set's size. Each batch is the read a page of the data set makes (the
/// read gate, renames, transformations, calculated fields, declared fields only), so a streamed row is the row a page would
/// have carried.
/// </para>
/// <para>
/// Batches are cut by offset, so the order has to identify each row: the caller's own sort, then the data set's key fields.
/// A data set that declares none has no such order, so it is read in a single query instead, which cannot repeat or drop a
/// row; that result is held in memory whole before it is written, which batching otherwise avoids. The first batch is read
/// before the response starts, so a refusal (a gate, an invalid filter, an unreachable connection) still answers with a status.
/// A batch that fails after that cannot change the status, and must not let the response pass for a whole one: the rows throw a
/// <see cref="DataQueryStreamException"/>, which leaves the JSON unfinished, and the endpoint aborts the connection. A response
/// that finished ends with <see cref="RowCount"/> and <see cref="Complete"/>, written after the rows, so only it carries them.
/// </para>
/// <para>
/// A read that filters or sorts on a calculated field is already held in memory, up to the calculated row cap, so it is read
/// whole in one go instead of once per batch.
/// </para>
/// </remarks>
public sealed class DataQueryStream
{
    private readonly DataSetQuery _query;
    private readonly IDataSetImplementationConfiguration _dataSet;
    private readonly QueryFilterNode? _filter;
    private readonly List<QuerySort> _order;
    private readonly int _skip;
    private readonly int _batchSize;
    private readonly int _rowCap;
    private readonly IGenericResult<QueryFilterNode?> _scoped;
    private readonly ILogger _logger;
    private readonly CancellationToken _cancellationToken;

    private DataQueryPage? _first;

    private DataQueryStream(
        DataSetQuery query, IDataSetImplementationConfiguration dataSet, DataQueryRequest request, List<QuerySort> order,
        int batchSize, int rowCap, IGenericResult<QueryFilterNode?> scoped, ILogger logger, CancellationToken cancellationToken)
    {
        _query = query;
        _dataSet = dataSet;
        _filter = request.Filter;
        _skip = request.Skip;
        _order = order;
        _batchSize = batchSize;
        _rowCap = rowCap;
        _scoped = scoped;
        _logger = logger;
        _cancellationToken = cancellationToken;
    }

    /// <summary>Gets the name of the data set read.</summary>
    public string DataSetName => _dataSet.Name;

    /// <summary>Gets the columns every row carries.</summary>
    public IReadOnlyList<DataQueryColumnDto> Columns { get; private set; } = [];

    /// <summary>Gets the failed batch's result when the rows ended in a <see cref="DataQueryStreamException"/>, or null while they have not.</summary>
    public IGenericResult? Failure { get; private set; }

    /// <summary>Gets how many rows have been handed to the response so far; once the rows are written, how many it carries.</summary>
    public int RowCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether every row has been handed to the response. True only when the last batch was written, so
    /// a response that reports it was finished and one that does not was cut off.
    /// </summary>
    public bool Complete { get; private set; }

    /// <summary>Gets the rows, each holding only the columns in <see cref="Columns"/>, read a batch at a time as they are enumerated.</summary>
    public IAsyncEnumerable<IReadOnlyDictionary<string, object?>> Rows => Read();

    /// <summary>Finds the stream's own failure in what was thrown while its rows were written.</summary>
    /// <param name="exception">What the writer of the response caught.</param>
    /// <returns>The <see cref="DataQueryStreamException"/> it is or wraps; null for any other exception.</returns>
    /// <remarks>The serializer may carry an exception from the rows as the inner one of its own.</remarks>
    public static DataQueryStreamException? CausedBy(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception as DataQueryStreamException is { } direct ? direct : exception.InnerException as DataQueryStreamException;
    }

    /// <summary>Opens the stream by reading its first batch, or refuses a query that cannot be read whole.</summary>
    /// <param name="query">The read every batch goes through.</param>
    /// <param name="dataSet">The data set, already resolved as the caller.</param>
    /// <param name="request">The caller's filter, sort and offset; its filter and paging are not changed.</param>
    /// <param name="batchSize">The rows read per batch; not a limit on the response.</param>
    /// <param name="rowCap">The most rows a read that filters or sorts on a calculated field may read (FDW-1045).</param>
    /// <param name="scope">The filter the route puts on every read of this data set; asked once.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">A token to cancel the operation; it also ends the rows when the client goes away.</param>
    /// <returns>The stream, or the refusal the caller is answered with.</returns>
    public static async Task<IGenericResult<DataQueryStream>> Open(
        DataSetQuery query,
        IDataSetImplementationConfiguration dataSet,
        DataQueryRequest request,
        int batchSize,
        int rowCap,
        Func<CancellationToken, Task<IGenericResult<QueryFilterNode?>>> scope,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(dataSet);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(logger);

        var scoped = await scope(cancellationToken).ConfigureAwait(false);
        if (scoped.IsFailure) return scoped.ToNewResult<DataQueryStream>();

        // Why the batch is every row when the data set declares no key: batches are cut by offset, which only reads each row
        // once when an order identifies it. Without one the single query is the only read that cannot repeat or drop a row,
        // at the cost of holding the result in memory before it is written.
        var keys = KeyFields(dataSet);
        var batch = keys.Count == 0 ? PaginatedListRequest.NoLimitTake : batchSize;

        var stream = new DataQueryStream(query, dataSet, request, Order(request, keys), batch, rowCap, scoped, logger, cancellationToken);
        var first = await stream.ReadFirst().ConfigureAwait(false);
        if (first.IsFailure) return first.ToNewResult<DataQueryStream>();

        stream._first = first.Value!;
        stream.Columns = first.Value!.Columns;
        EndpointLogger.DataQueryStreamOpened(logger, dataSet.Name, batch);
        return GenericResult<DataQueryStream>.Success(stream);
    }

    /// <summary>The data set's key fields, in ordinal order: what identifies a row, and so what batches can be cut by.</summary>
    private static List<string> KeyFields(IDataSetImplementationConfiguration dataSet)
        => dataSet.Fields
            .Where(f => f.IsCurrent && !f.IsDeleted && f.IsKey && !f.IsCalculated)
            .OrderBy(f => f.Ordinal)
            .Select(f => f.Name)
            .ToList();

    /// <summary>The order batches are cut in: the caller's sort, then each key field it did not name.</summary>
    private static List<QuerySort> Order(DataQueryRequest request, List<string> keys)
    {
        var order = request.Sort is null ? new List<QuerySort>() : request.Sort.OfType<QuerySort>().ToList();
        order.AddRange(keys
            .Where(key => !order.Any(sort => string.Equals(sort.Field, key, StringComparison.OrdinalIgnoreCase)))
            .Select(key => new QuerySort { Field = key, Direction = "Ascending" })
            .ToList());

        return order;
    }

    private async Task<IGenericResult<DataQueryPage>> ReadFirst()
    {
        var first = await Batch(_skip, _batchSize).ConfigureAwait(false);
        if (first.IsFailure || first.Value is not { InMemory: true, HasMoreRows: true })
            return first;

        // Already held in memory, up to the row cap: what is left is one read of the cap, not a read of the cap per batch.
        return await Batch(_skip, _rowCap).ConfigureAwait(false);
    }

    private Task<IGenericResult<DataQueryPage>> Batch(int skip, int take)
        => _query.Run(
            _dataSet,
            new DataQueryRequest { DataSetName = _dataSet.Name, Filter = _filter, Sort = _order, Skip = skip, Take = take },
            take,
            _rowCap,
            _ => Task.FromResult(_scoped),
            _cancellationToken);

    private async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> Read()
    {
        var page = _first;
        var skip = _skip;
        while (page is not null)
        {
            foreach (var row in page.Rows)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                RowCount++;
                yield return row;
            }

            if (!page.HasMoreRows)
                break;

            skip += page.Rows.Count;
            var next = await Batch(skip, _batchSize).ConfigureAwait(false);
            if (next.IsFailure)
            {
                // Why thrown and not an early end of the rows: ending them closes the array, and the response would read as a
                // whole one with a 200 on it. The exception tears through the serializer, leaving the JSON unfinished.
                Failure = next;
                EndpointLogger.DataQueryStreamFailed(_logger, _dataSet.Name, RowCount);
                throw new DataQueryStreamException(_dataSet.Name, RowCount, next);
            }

            page = next.Value;
        }

        Complete = true;
        EndpointLogger.DataQueryStreamCompleted(_logger, _dataSet.Name, RowCount);
    }
}
