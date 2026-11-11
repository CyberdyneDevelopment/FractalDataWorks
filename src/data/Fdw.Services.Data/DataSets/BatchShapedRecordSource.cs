using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// A source's cursor shaped into a data set's records a batch at a time (FDW-4911): each batch of the source's rows goes through the same
/// shaping a page of the data set goes through, so a streamed row and a paged row are the same row. It owns the cursor and disposes it.
/// </summary>
internal sealed class BatchShapedRecordSource : IRecordSource<DataRecord>
{
    private readonly IRecordSource<DataRecord> _cursor;
    private readonly Func<List<Dictionary<string, object?>>, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> _shape;
    private readonly int _batchSize;

    /// <summary>Initializes a new instance of the <see cref="BatchShapedRecordSource"/> class.</summary>
    /// <param name="schema">The data set's fields: what every record carries.</param>
    /// <param name="cursor">The source's cursor, its records under the source's names; owned from here on.</param>
    /// <param name="shape">Shapes one batch of the source's rows into the data set's rows.</param>
    /// <param name="batchSize">How many rows are shaped together; at least one.</param>
    public BatchShapedRecordSource(
        RecordSchema schema,
        IRecordSource<DataRecord> cursor,
        Func<List<Dictionary<string, object?>>, CancellationToken, Task<IGenericResult<IEnumerable<Dictionary<string, object?>>>>> shape,
        int batchSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        Schema = schema;
        _cursor = cursor;
        _shape = shape;
        _batchSize = batchSize;
    }

    /// <inheritdoc />
    public RecordSchema Schema { get; }

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read() => Read(CancellationToken.None).ToBlockingEnumerable();

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var batch = new List<Dictionary<string, object?>>(_batchSize);
        await foreach (var record in _cursor.Read(cancellationToken).ConfigureAwait(false))
        {
            if (record.IsFailure)
            {
                yield return record;
                yield break;
            }

            batch.Add(Row(record.Value));
            if (batch.Count < _batchSize)
                continue;

            await foreach (var shaped in Shape(batch, cancellationToken).ConfigureAwait(false))
            {
                yield return shaped;
                if (shaped.IsFailure)
                    yield break;
            }

            batch = new List<Dictionary<string, object?>>(_batchSize);
        }

        if (batch.Count > 0)
        {
            await foreach (var shaped in Shape(batch, cancellationToken).ConfigureAwait(false))
                yield return shaped;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _cursor.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _cursor.DisposeAsync();

    private static Dictionary<string, object?> Row(DataRecord record)
    {
        var row = new Dictionary<string, object?>(record.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < record.FieldCount; i++)
            row[record.Schema.GetFieldName(i)] = record[i];

        return row;
    }

    // A batch that fails to shape ends the stream with its failure; nothing of it goes out.
    private async IAsyncEnumerable<IGenericResult<DataRecord>> Shape(
        List<Dictionary<string, object?>> batch, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var shaped = await _shape(batch, cancellationToken).ConfigureAwait(false);
        if (shaped.IsFailure || shaped.Value is not { } rows)
        {
            yield return shaped.ToNewResult<DataRecord>();
            yield break;
        }

        foreach (var row in rows)
            yield return GenericResult<DataRecord>.Success(RowListRecordSource.Record(Schema, row));
    }
}
