using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
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
    private readonly MemoryStream _bridge;
    private readonly TextWriter _target;
    private readonly System.Text.Decoder _decoder = System.Text.Encoding.UTF8.GetDecoder();
    private byte[] _byteBuffer = new byte[1024];
    private char[] _charBuffer = new char[1024];
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
            WriteValue(_writer, kv.Value, kv.Key);
        }

        _writer.WriteEndObject();
        DrainBridge();
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
        DrainBridge();
        _target.Flush();
    }

    // Why the bridge is emptied after every record rather than read back once at the end: Utf8JsonWriter
    // emits bytes and the target takes characters, so a buffer has to sit between them - but holding the
    // WHOLE document there makes memory scale with the row count, which is exactly what a streaming write
    // must not do. The writer's array-depth state lives in the writer, not in the stream, so truncating
    // the stream after each flush preserves the framing.
    private void DrainBridge()
    {
        _writer.Flush();
        var byteCount = (int)_bridge.Length;
        if (byteCount == 0)
        {
            return;
        }

        if (_byteBuffer.Length < byteCount)
        {
            _byteBuffer = new byte[byteCount];
        }

        _bridge.Position = 0;
        var read = _bridge.Read(_byteBuffer, 0, byteCount);

        // Why the decoder is kept between drains: a multi-byte character straddling two drains would
        // otherwise decode as two replacement characters.
        var required = _decoder.GetCharCount(_byteBuffer, 0, read, flush: false);
        if (_charBuffer.Length < required)
        {
            _charBuffer = new char[required];
        }

        _target.Write(_charBuffer, 0, _decoder.GetChars(_byteBuffer, 0, read, _charBuffer, 0, flush: false));
        _bridge.SetLength(0);
        _bridge.Position = 0;
    }

    // Why only these types are written natively: they are the ones JSON itself has. Everything
    // else becomes the canonical text, which is the same text every other format writes - so a
    // value cannot mean one thing in a CSV export and another in a JSON one.
    private static void WriteValue(Utf8JsonWriter writer, object? value, string columnName)
    {
        switch (value)
        {
            case null:
            case DBNull:
                writer.WriteNullValue();
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case byte or sbyte or short or ushort or int or uint or long:
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case ulong unsigned:
                writer.WriteNumberValue(unsigned);
                break;
            case float or double or decimal:
                writer.WriteNumberValue(Convert.ToDecimal(value, CultureInfo.InvariantCulture));
                break;
            default:
                writer.WriteStringValue(RecordValueText.Format(value, columnName));
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
        _bridge.Dispose();
    }
}
