using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Rows already read whole, handed out as records over one schema (FDW-4911).
/// </summary>
/// <remarks>
/// For a read that has to hold its whole result before it can hand any of it on: an in-memory join, or a document a connection parses
/// whole. Each record carries the schema's fields in order; a field a row does not carry is null in that record, as it is in a cursor's
/// record (<see cref="CursorRecordSource"/>). It holds nothing to release.
/// </remarks>
public sealed class RowListRecordSource : IRecordSource<DataRecord>
{
    private readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> _rows;

    /// <summary>Initializes a new instance of the <see cref="RowListRecordSource"/> class.</summary>
    /// <param name="schema">The fields every record carries.</param>
    /// <param name="rows">The rows, in the order they are handed out.</param>
    public RowListRecordSource(RecordSchema schema, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _rows = rows ?? throw new ArgumentNullException(nameof(rows));
    }

    /// <inheritdoc />
    public RecordSchema Schema { get; }

    /// <summary>The fields the rows carry, in the order they first appear: the schema of rows read whole from a document (FDW-4911).</summary>
    /// <param name="rows">The rows.</param>
    /// <returns>One field for each name any row carries; none when there are no rows.</returns>
    public static RecordSchema SchemaOfRows(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        if (rows is null)
            throw new ArgumentNullException(nameof(rows));

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            foreach (var name in row.Keys)
            {
                if (seen.Add(name))
                    names.Add(name);
            }
        }

        var fields = new List<Fdw.Data.Abstractions.IDataField>(names.Count);
        for (var i = 0; i < names.Count; i++)
            fields.Add(new RecordField(names[i], i));

        return new RecordSchema(fields);
    }

    /// <summary>Makes the record of one row: the schema's fields in order, null for a field the row does not carry.</summary>
    /// <param name="schema">The fields the record carries.</param>
    /// <param name="row">The row.</param>
    /// <returns>The record.</returns>
    public static DataRecord Record(RecordSchema schema, IReadOnlyDictionary<string, object?> row)
    {
        if (schema is null)
            throw new ArgumentNullException(nameof(schema));

        if (row is null)
            throw new ArgumentNullException(nameof(row));

        var values = new object?[schema.FieldCount];
        for (var i = 0; i < values.Length; i++)
            values[i] = row.TryGetValue(schema.GetFieldName(i), out var value) ? value : null;

        return new DataRecord(schema, values);
    }

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read()
    {
        foreach (var row in _rows)
            yield return GenericResult<DataRecord>.Success(Record(Schema, row));
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var row in _rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return GenericResult<DataRecord>.Success(Record(Schema, row));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => default;
}
