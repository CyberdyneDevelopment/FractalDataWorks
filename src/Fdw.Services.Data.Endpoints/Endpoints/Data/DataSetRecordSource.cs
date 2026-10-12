using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Runtime;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// A data set's rows as a record source, read page by page through <see cref="DataSetQuery"/> (FDW-1075).
/// </summary>
/// <remarks>
/// <para>
/// A streamed response reads the data set the same way a page of it is read: the read gate, the strategy's renames,
/// transformations and calculated fields, and only the declared fields. Each record carries the declared fields in
/// ordinal order; a field a row does not carry is null in that record.
/// </para>
/// <para>
/// Pages are read in the order of the data set's key fields, because only an order that identifies each row reads every
/// row exactly once across pages. A data set that declares no key field is refused, and says so, rather than read in an
/// order that could repeat or drop rows between pages.
/// </para>
/// </remarks>
public sealed class DataSetRecordSource : IRecordSource<DataRecord>
{
    private readonly DataSetQuery _query;
    private readonly IDataSetImplementationConfiguration _dataSet;
    private readonly List<QuerySort> _order;
    private readonly int _pageSize;
    private readonly int _rowCap;

    private DataSetRecordSource(
        DataSetQuery query, IDataSetImplementationConfiguration dataSet, RecordSchema schema, List<QuerySort> order, int pageSize, int rowCap)
    {
        _query = query;
        _dataSet = dataSet;
        Schema = schema;
        _order = order;
        _pageSize = pageSize;
        _rowCap = rowCap;
    }

    /// <inheritdoc />
    public RecordSchema Schema { get; }

    /// <summary>Opens a data set's rows for reading, or refuses a data set that cannot be read page by page.</summary>
    /// <param name="query">The read every page goes through.</param>
    /// <param name="dataSet">The data set, already resolved as the caller.</param>
    /// <param name="pageSize">The rows read per page, resolved from the paging settings.</param>
    /// <param name="rowCap">The most rows a read that filters or sorts on a calculated field may read (FDW-1045).</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The record source, or the refusal the caller is answered with.</returns>
    public static IGenericResult<IRecordSource<DataRecord>> Open(
        DataSetQuery query, IDataSetImplementationConfiguration dataSet, int pageSize, int rowCap, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(dataSet);
        ArgumentNullException.ThrowIfNull(logger);

        var declared = dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted).OrderBy(f => f.Ordinal).ToList();
        var order = declared
            .Where(f => f.IsKey && !f.IsCalculated)
            .Select(f => new QuerySort { Field = f.Name, Direction = "Ascending" })
            .ToList();
        if (order.Count == 0)
            return new DataQueryRefusals(dataSet.Name, logger).Invalid<IRecordSource<DataRecord>>(
                "a data set is streamed page by page in the order of its key fields, and it declares none; mark the field or fields that identify a row as keys");

        var schema = new RecordSchema([.. declared.Select(f => (Fdw.Data.Abstractions.IDataField)new DataSetRuntimeField(f))]);
        return GenericResult<IRecordSource<DataRecord>>.Success(new DataSetRecordSource(query, dataSet, schema, order, pageSize, rowCap));
    }

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read() => Read(CancellationToken.None).ToBlockingEnumerable();

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var skip = 0;
        while (true)
        {
            var request = new DataQueryRequest { DataSetName = _dataSet.Name, Skip = skip, Take = _pageSize, Sort = [.. _order] };
            var page = await _query.Run(_dataSet, request, _pageSize, _rowCap, DataSetQuery.Unscoped, cancellationToken).ConfigureAwait(false);
            if (page.IsFailure)
            {
                yield return page.ToNewResult<DataRecord>();
                yield break;
            }

            var read = page.Value!;
            foreach (var row in read.Rows)
                yield return GenericResult<DataRecord>.Success(Record(row));

            if (!read.HasMoreRows)
                yield break;

            skip += _pageSize;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private DataRecord Record(IReadOnlyDictionary<string, object?> row)
    {
        var values = new object?[Schema.FieldCount];
        for (var i = 0; i < values.Length; i++)
            values[i] = row.TryGetValue(Schema.GetFieldName(i), out var value) ? value : null;

        return new DataRecord(Schema, values);
    }
}
