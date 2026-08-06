using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using RecordParser.Builders.Writer;
using RecordParser.Extensions;

namespace Fdw.Data.RowSources.FixedWidth.Abstractions;

/// <summary>
/// Writes flat name→value rows as fixed-width (fixed-length) text using RecordParser's
/// <see cref="WriterExtensions.WriteRecords{T}(TextWriter, IEnumerable{T}, TryFormat{T})"/> driver.
/// The write-side mirror of <see cref="FixedWidthStreamRowSource"/>.
/// </summary>
/// <remarks>
/// Each field is laid into its <see cref="FixedWidthField.StartIndex"/>/<see cref="FixedWidthField.Length"/>
/// window, padded to width with <see cref="FixedWidthField.PaddingChar"/> on the side indicated by
/// <see cref="FixedWidthField.Padding"/>. Reading the written output back through
/// <see cref="FixedWidthStreamRowSource"/> with the same field list yields the original (trimmed) rows.
/// </remarks>
public sealed class FixedWidthStreamRowWriter : IRowWriter
{
    private readonly TextWriter _target;
    private readonly FixedWidthRowWriterOptions _options;
    private readonly int _recordWidth;
    private readonly TryFormat<IReadOnlyDictionary<string, object?>> _formatRow;
    private bool _headerWritten;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWidthStreamRowWriter"/> class.
    /// </summary>
    /// <param name="target">The destination text writer.</param>
    /// <param name="options">Fixed-width writer options (field definitions are required).</param>
    /// <exception cref="ArgumentException">Thrown when no field definitions are supplied.</exception>
    public FixedWidthStreamRowWriter(TextWriter target, FixedWidthRowWriterOptions? options = null)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _options = options ?? new FixedWidthRowWriterOptions();
        if (_options.Fields.Count == 0)
        {
            // Why: NO FALLBACKS — field layout is a real serializer input.
            throw new ArgumentException(
                "FixedWidthRowWriterOptions.Fields must contain at least one field definition.", nameof(options));
        }

        var width = 0;
        foreach (var f in _options.Fields)
        {
            var end = f.StartIndex + f.Length;
            if (end > width) width = end;
        }

        _recordWidth = width;
        _formatRow = FormatRow;
    }

    /// <inheritdoc />
    public void Write(IReadOnlyDictionary<string, object?> row)
    {
        if (row is null) throw new ArgumentNullException(nameof(row));
        EnsureHeader();
        WriterExtensions.WriteRecords(_target, [row], _formatRow);
    }

    /// <inheritdoc />
    public void WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        if (rows is null) throw new ArgumentNullException(nameof(rows));
        EnsureHeader();
        WriterExtensions.WriteRecords(_target, rows, _formatRow);
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
        cancellationToken.ThrowIfCancellationRequested();
        EnsureHeader();
        WriterExtensions.WriteRecords(_target, rows, _formatRow);
        return default;
    }

    /// <inheritdoc />
    // Why: the typed IRecordWriter<DataRecord> surface projects the row record's field-array onto the
    // existing dictionary write path; the flyweight schema names the fields laid into the fixed-width windows.
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
        EnsureHeader();
        _target.Flush();
    }

    private void EnsureHeader()
    {
        if (_headerWritten) return;
        _headerWritten = true;
        if (_options.WriteHeader)
        {
            var headerRow = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var f in _options.Fields) headerRow[f.Name] = f.Name;
            WriterExtensions.WriteRecords(_target, [headerRow], _formatRow);
        }
    }

    // Why: RecordParser's TryFormat contract — lay the fixed-width record into the destination span.
    private bool FormatRow(IReadOnlyDictionary<string, object?> row, Span<char> destination, out int charsWritten)
    {
        if (_recordWidth > destination.Length)
        {
            charsWritten = 0;
            return false;
        }

        var window = destination.Slice(0, _recordWidth);
        window.Fill(' ');

        foreach (var field in _options.Fields)
        {
            var raw = row.TryGetValue(field.Name, out var v)
                ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty
                : string.Empty;

            LayField(window.Slice(field.StartIndex, field.Length), raw, field);
        }

        charsWritten = _recordWidth;
        return true;
    }

    private static void LayField(Span<char> cell, string value, FixedWidthField field)
    {
        cell.Fill(field.PaddingChar);

        // Why: a value wider than the cell is truncated to the cell width (right side dropped),
        // matching the read path which only slices the field's window.
        var text = value.Length > cell.Length ? value.AsSpan(0, cell.Length) : value.AsSpan();

        if (field.Padding == Padding.Left)
        {
            // Pad on the left → value is right-aligned.
            text.CopyTo(cell.Slice(cell.Length - text.Length));
        }
        else
        {
            // Pad on the right → value is left-aligned.
            text.CopyTo(cell);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        EnsureHeader();
        _target.Flush();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
