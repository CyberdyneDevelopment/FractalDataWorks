using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Json.Abstractions;

/// <summary>
/// Writes flat name→value rows as a JSON array of objects using <see cref="Utf8JsonWriter"/>.
/// The write-side mirror of <see cref="JsonStreamRowSource"/>.
/// </summary>
/// <remarks>
/// Output shape is a single top-level array whose elements are objects with one property per
/// field — the exact inverse of what <see cref="JsonStreamRowSource"/> reads from a root array.
/// Reading the written output back through <see cref="JsonStreamRowSource"/> yields the original rows.
/// <para>
/// Implements <see cref="IRowWriter"/> (not just the item-writer <see cref="IRecordWriter{T}"/> surface):
/// unlike Delimited/FixedWidth, where <see cref="IRowWriter"/> exists because the target has a genuinely
/// fixed COLUMN layout, JSON has no such structural constraint — a JSON object can carry any set of
/// properties. Writing through <see cref="Write(IReadOnlyDictionary{string, object?})"/> directly (the
/// <see cref="IRowWriter"/> path) preserves EVERY key in the row dictionary, symmetric with
/// <see cref="JsonStreamRowSource"/>'s read side, which decodes every JSON property dynamically with no
/// schema projection. Going through the item-writer path instead (<see cref="Write(DataRecord)"/> via
/// <see cref="DataRecord.ToDictionary"/>) would silently DROP any row key the container's declared field
/// schema doesn't carry, because a <see cref="DataRecord"/>'s value array is dimensioned to the schema's
/// field count — a real data-loss bug for a JSON config file with columns beyond its declared schema
/// (e.g. audit columns from an external seed/import). Callers that need the full-fidelity write MUST
/// route through the <see cref="IRowWriter"/> surface, not the <see cref="IRecordWriter{T}"/> one.
/// </para>
/// </remarks>
public sealed class JsonStreamRowWriter : IRowWriter
{
    private readonly Utf8JsonWriter _writer;
    private readonly Stream _bridge;
    private readonly TextWriter _target;
    private bool _arrayStarted;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonStreamRowWriter"/> class.
    /// </summary>
    /// <param name="target">The destination text writer.</param>
    /// <param name="options">JSON writer options.</param>
    public JsonStreamRowWriter(TextWriter target, JsonRowWriterOptions? options = null)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        var opts = options ?? new JsonRowWriterOptions();
        // Why: Utf8JsonWriter targets a Stream/IBufferWriter, but IRowWriter writes to a TextWriter
        // (so all formats share one target abstraction). Buffer to a MemoryStream, then decode UTF-8
        // to the TextWriter on Flush — JSON output is always UTF-8, so the decode is lossless.
        _bridge = new MemoryStream();
        _writer = new Utf8JsonWriter(_bridge, new JsonWriterOptions
        {
            Indented = opts.WriteIndented,
            SkipValidation = opts.SkipValidation
        });
    }

    /// <inheritdoc />
    public void Write(IReadOnlyDictionary<string, object?> row)
    {
        if (row is null) throw new ArgumentNullException(nameof(row));
        if (!_arrayStarted)
        {
            _writer.WriteStartArray();
            _arrayStarted = true;
        }

        _writer.WriteStartObject();
        foreach (var kv in row)
        {
            _writer.WritePropertyName(kv.Key);
            WriteValue(_writer, kv.Value);
        }

        _writer.WriteEndObject();
    }

    /// <inheritdoc />
    public void WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        foreach (var row in rows)
        {
            Write(row);
        }
    }

    /// <summary>
    /// Writes all rows in the supplied sequence to the target.
    /// </summary>
    /// <param name="rows">The rows to write as flat name→value maps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all rows have been written.</returns>
    public ValueTask WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(row);
        }

        return default;
    }

    /// <inheritdoc />
    // Why: the typed IRecordWriter<DataRecord> surface projects the record's field-array onto the
    // existing dictionary write path via DataRecord.ToDictionary() — the flyweight schema names the
    // emitted JSON properties. No second serializer path.
    public void Write(DataRecord record) => Write(record.ToDictionary());

    /// <inheritdoc />
    public void Write(IEnumerable<DataRecord> records)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        foreach (var record in records)
        {
            Write(record);
        }
    }

    /// <inheritdoc />
    public async ValueTask Write(IAsyncEnumerable<DataRecord> records, CancellationToken cancellationToken = default)
    {
        if (records is null) throw new ArgumentNullException(nameof(records));
        await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Write(record);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        if (!_arrayStarted)
        {
            _writer.WriteStartArray();
            _arrayStarted = true;
        }

        _writer.WriteEndArray();
        _writer.Flush();

        _bridge.Position = 0;
        using var reader = new StreamReader(_bridge, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        _target.Write(reader.ReadToEnd());
        _target.Flush();
    }

    // Why: writes a CLR value as its natural JSON token, inverse of JsonStreamRowSource's
    // JsonValueKind switch — strings, the numeric family, bool, and null. Anything else is
    // emitted as its string form (the read path stores raw text for non-scalar tokens too).
    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case byte or sbyte or short or ushort or int or uint or long:
                writer.WriteNumberValue(Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
            case ulong u:
                writer.WriteNumberValue(u);
                break;
            case float or double or decimal:
                writer.WriteNumberValue(Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
            default:
                writer.WriteStringValue(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writer.Dispose();
        _bridge.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _writer.DisposeAsync().ConfigureAwait(false);
        // Why: Stream.DisposeAsync is not available on netstandard2.0 (the target of this abstractions
        // project); MemoryStream's sync Dispose is fully sufficient (no unflushed OS handles).
        _bridge.Dispose();
    }
}
