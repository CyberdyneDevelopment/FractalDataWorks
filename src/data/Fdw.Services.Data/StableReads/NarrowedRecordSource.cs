using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// A connection's cursor cut to the fields the same query returns when it is read whole (FDW-4911).
/// </summary>
/// <remarks>
/// A connection's cursor builds its records over every field the container declares, and a field its statement did not select is null in
/// every record: a key column, a metadata column. A read of the same query through <c>Execute</c> returns only the selected fields. Cutting
/// the cursor's records to those fields gives a streamed read the shape of a whole one, so a pipeline sees the same columns whichever way the
/// rows reach it. It owns the cursor and disposes it.
/// </remarks>
public sealed class NarrowedRecordSource : IRecordSource<DataRecord>
{
    private readonly IRecordSource<DataRecord> _inner;
    private readonly int[] _ordinals;

    /// <summary>Initializes a new instance of the <see cref="NarrowedRecordSource"/> class.</summary>
    /// <param name="inner">The cursor; owned from here on.</param>
    /// <param name="fieldNames">The fields to keep, matched to the cursor's by name, ordinal and case-insensitive as the statement names them.</param>
    public NarrowedRecordSource(IRecordSource<DataRecord> inner, IReadOnlyCollection<string> fieldNames)
    {
        _inner = inner;

        var kept = new HashSet<string>(fieldNames, StringComparer.OrdinalIgnoreCase);
        var fields = inner.Schema.Fields;
        _ordinals = [.. Enumerable.Range(0, fields.Count).Where(i => kept.Contains(fields[i].Name))];
        Schema = new RecordSchema([.. _ordinals.Select(i => fields[i])]);
    }

    /// <inheritdoc />
    public RecordSchema Schema { get; }

    /// <inheritdoc />
    public IEnumerable<IGenericResult<DataRecord>> Read()
    {
        foreach (var record in _inner.Read())
            yield return Narrow(record);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IGenericResult<DataRecord>> Read([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var record in _inner.Read(cancellationToken).ConfigureAwait(false))
            yield return Narrow(record);
    }

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _inner.DisposeAsync();

    private IGenericResult<DataRecord> Narrow(IGenericResult<DataRecord> record)
    {
        if (record.IsFailure)
            return record;

        var source = record.Value.Values;
        var values = new object?[_ordinals.Length];
        for (var i = 0; i < values.Length; i++)
            values[i] = source[_ordinals[i]];

        return GenericResult<DataRecord>.Success(new DataRecord(Schema, values));
    }
}
