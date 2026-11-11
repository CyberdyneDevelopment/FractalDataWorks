using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Logging;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Runtime;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// How a gateway reads a container or a data set as a stream of rows in a stable order (FDW-4911): the one place the choice is made, so
/// every gateway makes it the same way.
/// </summary>
/// <remarks>
/// <para>
/// A container is read through the connection's cursor when the connection can open one and the choice below takes it; otherwise, when
/// its connection states that the store keeps its own order (<c>IStoreOrderedConnection</c>: a remote API paged by its protocol, a file),
/// in pages in that order; otherwise in keyset pages by every field of the primary key the container declares; otherwise it is refused
/// (<c>StableReadUnavailable</c>). It is never read in pages of no stated order, and no key is guessed.
/// </para>
/// <para>
/// A data set is read in keyset pages by its key fields when its kind pages in the store; a data set of that kind that declares no key is
/// refused (<c>DataSetNotPageable</c>). A kind that does not page in the store (an in-memory join) builds its whole set to answer any read,
/// so it is read once, unpaged, and refused when it holds more rows than <see cref="DataGatewayServiceTypes.UnpagedDataSetRowCap"/>.
/// </para>
/// <para>
/// The page size and the row cap are configuration, declared on <see cref="DataGatewayServiceTypes"/> and refused below one at registration
/// and again here.
/// </para>
/// </remarks>
public static class StableRead
{
    /// <summary>Opens a container's rows for reading in a stable order.</summary>
    /// <param name="command">The read; a query, whose own ordering and paging are not used by a keyset read.</param>
    /// <param name="container">The container, already resolved, with its fields and keys.</param>
    /// <param name="connection">The connection the container's store reaches it by.</param>
    /// <param name="readPage">Runs one keyset page on that connection, as the gateway runs any command on it.</param>
    /// <param name="target">What is read, for the messages: the container and its store.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows as a record source the caller disposes, or why they cannot be read in a stable order.</returns>
    public static async Task<IGenericResult<IRecordSource<DataRecord>>> OpenContainer(
        IDataCommand command,
        IDataContainer container,
        IDataConnection connection,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> readPage,
        string target,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (command is not IQueryCommand query)
            return NeedsQuery(command, target, logger);

        if (ReadsThroughCursor(connection) is { } cursorConnection)
        {
            StableReadLog.ReadingThroughCursor(logger, target);
            var opened = await cursorConnection.OpenRecordSource(command, container, cancellationToken).ConfigureAwait(false);
            return opened.IsSuccess && opened.Value is { } cursor && ReturnedFieldNames(query, container) is { Count: > 0 } returned
                ? GenericResult<IRecordSource<DataRecord>>.Success(new NarrowedRecordSource(cursor, returned))
                : opened;
        }

        return connection is IStoreOrderedConnection storeOrdered
            ? InStoreOrder(query, container, readPage, storeOrdered.StoreOrder, target, logger)
            : Keyset(query, container, readPage, target, logger);
    }

    /// <summary>Opens a data set's rows for reading in a stable order, through the kind that runs it.</summary>
    /// <param name="command">The read; a query, whose own ordering and paging are not used.</param>
    /// <param name="kind">The data set's kind: it runs each read, and, when it can (<see cref="IStreamingDataSetType"/>), streams the set.</param>
    /// <param name="context">The data set, resolved and admitted, and what reaching its sources needs.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows as a record source the caller disposes, or why they cannot be read in a stable order.</returns>
    public static Task<IGenericResult<IRecordSource<DataRecord>>> OpenDataSet(
        IDataCommand command,
        IDataSetType kind,
        DataSetExecutionContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        return OpenDataSet(
            command,
            context.Config,
            (read, ct) => kind.Execute<IEnumerable<Dictionary<string, object?>>>(context, read, ct),
            kind is IStreamingDataSetType streaming ? (batchSize, ct) => streaming.OpenStream(context, command, batchSize, ct) : null,
            logger,
            cancellationToken);
    }

    /// <summary>Opens a data set's rows for reading in a stable order.</summary>
    /// <param name="command">The read; a query, whose own ordering and paging are not used.</param>
    /// <param name="dataSet">The data set, already resolved and admitted.</param>
    /// <param name="execute">Runs one read of the data set through its kind, as the gateway runs any data set command.</param>
    /// <param name="stream">
    /// Opens the data set as one stream in batches of the size given, answering no source when it cannot be streamed; null for a kind that
    /// never streams.
    /// </param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows as a record source the caller disposes, or why they cannot be read in a stable order.</returns>
    /// <remarks>
    /// The same order as a container's: a stream when the data set's connection can open one, else keyset pages by its key fields when its
    /// kind pages in the store, else (an in-memory join) one unpaged read under the cap; a data set that pages in the store, declares no key
    /// and cannot be streamed is refused.
    /// </remarks>
    public static async Task<IGenericResult<IRecordSource<DataRecord>>> OpenDataSet(
        IDataCommand command,
        IDataSetImplementationConfiguration dataSet,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> execute,
        Func<int, CancellationToken, Task<IGenericResult<IRecordSource<DataRecord>?>>>? stream,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (command is not IQueryCommand query)
            return NeedsQuery(command, dataSet.Name, logger);

        var pageSize = PageSize(logger);
        if (pageSize.IsFailure)
            return pageSize.ToNewResult<IRecordSource<DataRecord>>();

        if (stream is not null)
        {
            var streamed = await stream(pageSize.Value, cancellationToken).ConfigureAwait(false);
            if (streamed.IsFailure)
                return streamed.ToNewResult<IRecordSource<DataRecord>>();

            if (streamed.Value is { } source)
            {
                StableReadLog.ReadingThroughCursor(logger, $"data set '{dataSet.Name}'");
                return GenericResult<IRecordSource<DataRecord>>.Success(source);
            }
        }

        // Why a kind that does not page is read whole: it builds its whole set to answer any read, so a page of it is cut from a set already held.
        if (DataSetReadGate.Pageable(dataSet, logger).IsFailure)
            return await Unpaged(query, dataSet.Name, DataSetSchema(dataSet), execute, logger, cancellationToken).ConfigureAwait(false);

        var keys = DeclaredFields(dataSet).Where(f => f.IsKey && !f.IsCalculated).Select(f => f.Name).ToList();
        if (keys.Count == 0)
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                DataServiceResultCodes.ByName("DataSetNotPageable"), logger,
                ResultDetails.Create("dataSet", dataSet.Name,
                    "reason", "its connection cannot stream it, and it is read in pages in the order of its key fields and declares none; mark the fields that identify a row as keys"));

        StableReadLog.ReadingInKeysetPages(logger, $"data set '{dataSet.Name}'", string.Join(", ", keys), pageSize.Value);
        return GenericResult<IRecordSource<DataRecord>>.Success(
            new KeysetRecordSource(DataSetSchema(dataSet), execute, query, keys, includeKeys: false, pageSize.Value, dataSet.Name, logger));
    }

    /// <summary>The fields every record of a data set carries: its current declared fields, in ordinal order.</summary>
    /// <param name="dataSet">The data set.</param>
    /// <returns>The schema.</returns>
    internal static RecordSchema DataSetSchema(IDataSetImplementationConfiguration dataSet) =>
        new([.. DeclaredFields(dataSet).Select(f => (Fdw.Data.Abstractions.IDataField)new DataSetRuntimeField(f))]);

    private static List<DataSetFieldConfiguration> DeclaredFields(IDataSetImplementationConfiguration dataSet) =>
        [.. dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted).OrderBy(f => f.Ordinal)];

    // THE choice between a connection's cursor and pages, made here and nowhere else: a connection that can open a cursor is read through
    // it. A store whose connection states that it keeps its own order is paged in that order; keyset pages read every other container that
    // declares a key.
    private static IRecordSourceConnection? ReadsThroughCursor(IDataConnection connection) => connection as IRecordSourceConnection;

    // Pages in the order the store keeps, which its connection states: the ordering guarantee is the store's, not the gateway's.
    private static IGenericResult<IRecordSource<DataRecord>> InStoreOrder(
        IQueryCommand query,
        IDataContainer container,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> readPage,
        string storeOrder,
        string target,
        ILogger logger)
    {
        var returned = ReturnedFieldNames(query, container);
        var fields = container.Schema.Fields.OfType<Fdw.Data.Abstractions.IDataField>().Where(f => returned.Contains(f.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        if (fields.Count == 0)
            return Unavailable(target, "it is read in pages in its store's own order and declares no fields to read", logger);

        var pageSize = PageSize(logger);
        if (pageSize.IsFailure)
            return pageSize.ToNewResult<IRecordSource<DataRecord>>();

        StableReadLog.ReadingInStorePages(logger, target, storeOrder, pageSize.Value);
        return GenericResult<IRecordSource<DataRecord>>.Success(new StoreOrderedRecordSource(new RecordSchema(fields), readPage, query, pageSize.Value, target, logger));
    }

    private static IGenericResult<IRecordSource<DataRecord>> Keyset(
        IQueryCommand query,
        IDataContainer container,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> readPage,
        string target,
        ILogger logger)
    {
        var keys = PrimaryKeyFieldNames(container);
        if (keys.Count == 0)
            return Unavailable(target, "its connection cannot open a cursor and it declares no primary key; declare the fields that identify a row as its primary key, or read it through a connection that streams", logger);

        if (query.Projection?.PropertyNames is { Count: > 0 } projected
            && keys.FirstOrDefault(key => !projected.Contains(key, StringComparer.OrdinalIgnoreCase)) is { } missing)
            return Unavailable(target, $"it is read in pages by its primary key, and the query's projection leaves out key field '{missing}'", logger);

        var returned = ReturnedFieldNames(query, container);
        var fields = container.Schema.Fields.OfType<Fdw.Data.Abstractions.IDataField>().Where(f => returned.Contains(f.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        if (fields.Count == 0)
            return Unavailable(target, "it is read in pages by its primary key and declares no fields to read", logger);

        var pageSize = PageSize(logger);
        if (pageSize.IsFailure)
            return pageSize.ToNewResult<IRecordSource<DataRecord>>();

        // Why the pages may ask for keys the query leaves out: each page is read after the last row's key, so every row carries it; the
        // records keep only the fields the query returns.
        var includeKeys = keys.Any(key => !returned.Contains(key, StringComparer.OrdinalIgnoreCase));
        StableReadLog.ReadingInKeysetPages(logger, target, string.Join(", ", keys), pageSize.Value);
        return GenericResult<IRecordSource<DataRecord>>.Success(
            new KeysetRecordSource(new RecordSchema(fields), readPage, query, keys, includeKeys, pageSize.Value, target, logger));
    }

    private static async Task<IGenericResult<IRecordSource<DataRecord>>> Unpaged(
        IQueryCommand query,
        string dataSet,
        RecordSchema schema,
        Func<IDataCommand, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> execute,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var cap = DataGatewayServiceTypes.UnpagedDataSetRowCap;
        if (cap < 1)
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                DataServiceResultCodes.ByName("UnpagedDataSetRowCapInvalid"), logger, ResultDetails.Create("rowCap", cap));

        StableReadLog.ReadingUnpaged(logger, dataSet, cap);

        var read = await execute(
            new QueryCommand<Dictionary<string, object?>>
            {
                Filter = query.Filter,
                Projection = query.Projection,
                IncludeMetadata = query.IncludeMetadata,
                IncludeDescriptions = query.IncludeDescriptions,
                IncludeKeys = query.IncludeKeys,
            },
            cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<IRecordSource<DataRecord>>();

        List<IReadOnlyDictionary<string, object?>> rows = read.Value is { } answered ? [.. answered] : [];
        return rows.Count > cap
            ? GenericResult<IRecordSource<DataRecord>>.Failure(
                DataServiceResultCodes.ByName("UnpagedDataSetOverRowCap"), logger,
                ResultDetails.Create("dataSet", dataSet, "rowCount", rows.Count, "rowCap", cap))
            : GenericResult<IRecordSource<DataRecord>>.Success(new RowListRecordSource(schema, rows));
    }

    // Every field of the container's first primary key, in the key's order: the order its rows are paged in. One field alone is not a
    // total order when the key has more (FDW-4911).
    private static List<string> PrimaryKeyFieldNames(IDataContainer container) =>
        container.Keys.FirstOrDefault(key => key.KeyType.IsPrimaryKey && key.KeyFields.Count > 0) is { } primary
            ? [.. primary.KeyFields.Select(field => field.LocalField.Name)]
            : [];

    // The fields a read of this query returns: its projection when it states one, else what each field's role returns for what the query includes.
    internal static List<string> ReturnedFieldNames(IQueryCommand query, IDataContainer container) =>
        query.Projection?.PropertyNames is { Count: > 0 } projected
            ? [.. projected]
            : [.. container.Schema.GetReturnedFields(query.IncludeMetadata, query.IncludeDescriptions, query.IncludeKeys).Select(f => f.Name)];

    private static IGenericResult<int> PageSize(ILogger logger)
    {
        var pageSize = DataGatewayServiceTypes.StableReadPageSize;
        return pageSize < 1
            ? GenericResult<int>.Failure(DataServiceResultCodes.ByName("StableReadPageSizeInvalid"), logger, ResultDetails.Create("pageSize", pageSize))
            : GenericResult<int>.Success(pageSize);
    }

    private static IGenericResult<IRecordSource<DataRecord>> Unavailable(string target, string reason, ILogger logger) =>
        GenericResult<IRecordSource<DataRecord>>.Failure(
            DataServiceResultCodes.ByName("StableReadUnavailable"), logger, ResultDetails.Create("target", target, "reason", reason));

    private static IGenericResult<IRecordSource<DataRecord>> NeedsQuery(IDataCommand command, string target, ILogger logger) =>
        GenericResult<IRecordSource<DataRecord>>.Failure(
            DataServiceResultCodes.ByName("StableReadNeedsQuery"), logger, ResultDetails.Create("target", target, "commandType", command.GetType().Name));
}
