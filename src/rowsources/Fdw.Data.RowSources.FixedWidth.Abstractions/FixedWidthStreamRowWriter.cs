using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
/// <see cref="FixedWidthStreamRowSource"/> with the same field list yields the original (trimmed) rows. The target writer is not owned:
/// whoever made it and asked the format for this writer disposes it.
/// </remarks>
public sealed class FixedWidthStreamRowWriter : IRowWriter
{
    [SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Not owned: the target writer; whoever made it and asked the format for this writer (the response sink's DrainingTextWriter, a file writer) disposes it.")]
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
    public FixedWidthStreamRowWriter(TextWriter target, FixedWidthRowWriterOptions options)
    {
        _target = target;
        _options = options;
        if (_options.Fields.Count == 0)
        {
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
        EnsureHeader();
        WriterExtensions.WriteRecords(_target, [row], _formatRow);
    }

    /// <inheritdoc />
    public void WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
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
        cancellationToken.ThrowIfCancellationRequested();
        EnsureHeader();
        WriterExtensions.WriteRecords(_target, rows, _formatRow);
        return default;
    }

    /// <inheritdoc />
    public void Write(DataRecord record) => Write(record.ToDictionary());

    /// <inheritdoc />
    public void Write(IEnumerable<DataRecord> records)
    {
        foreach (var record in records)
        {
            Write(record);
        }
    }

    /// <inheritdoc />
    public async ValueTask Write(IAsyncEnumerable<DataRecord> records, CancellationToken cancellationToken = default)
    {
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
            var raw = row.TryGetValue(field.Name, out var v) && !RecordValueText.IsAbsent(v)
                ? RecordValueText.Format(v!, field.Name)
                : string.Empty;

            LayField(window.Slice(field.StartIndex, field.Length), raw, field);
        }

        charsWritten = _recordWidth;
        return true;
    }

    private static void LayField(Span<char> cell, string value, FixedWidthField field)
    {
        cell.Fill(field.PaddingChar);

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
