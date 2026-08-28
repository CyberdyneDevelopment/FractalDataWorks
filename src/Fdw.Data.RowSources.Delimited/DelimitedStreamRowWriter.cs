using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.RowSources.Abstractions;
using RecordParser.Extensions;

namespace Fdw.Data.RowSources.Delimited.Abstractions;

/// <summary>
/// Writes flat name→value rows as delimited (CSV / variable-length) text using RecordParser's
/// <see cref="WriterExtensions.WriteRecords{T}(TextWriter, IEnumerable{T}, TryFormat{T})"/> driver.
/// The write-side mirror of <see cref="DelimitedStreamRowSource"/>.
/// </summary>
/// <remarks>
/// Columns are emitted in <see cref="RowWriterOptions.Columns"/> order; a value is quoted with
/// <see cref="DelimitedRowWriterOptions.QuoteChar"/> (and its embedded quotes doubled) when it
/// contains the separator, the quote, or a line break — matching RecordParser's
/// <c>ContainsQuotedFields</c> read convention. Reading the written output back through
/// <see cref="DelimitedStreamRowSource"/> with the same column list yields the original rows.
/// </remarks>
public sealed class DelimitedStreamRowWriter : IRowWriter
{
    private readonly TextWriter _target;
    private readonly DelimitedRowWriterOptions _options;
    private readonly List<string> _columns;
    private readonly TryFormat<IReadOnlyDictionary<string, object?>> _formatRow;
    private bool _headerWritten;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedStreamRowWriter"/> class.
    /// </summary>
    /// <param name="target">The destination text writer.</param>
    /// <param name="options">Delimited writer options (column names are required).</param>
    /// <exception cref="ArgumentException">Thrown when no column names are supplied.</exception>
    public DelimitedStreamRowWriter(TextWriter target, DelimitedRowWriterOptions? options = null)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _options = options ?? new DelimitedRowWriterOptions();
        if (_options.Columns.Count == 0)
        {
            throw new ArgumentException(
                "DelimitedRowWriterOptions.Columns must contain at least one column name.", nameof(options));
        }

        _columns = [.. _options.Columns];
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
            foreach (var c in _columns) headerRow[c] = c;
            WriterExtensions.WriteRecords(_target, [headerRow], _formatRow);
        }
    }

    private bool FormatRow(IReadOnlyDictionary<string, object?> row, Span<char> destination, out int charsWritten)
    {
        var line = BuildLine(row);
        if (line.Length > destination.Length)
        {
            charsWritten = 0;
            return false;
        }

        line.AsSpan().CopyTo(destination);
        charsWritten = line.Length;
        return true;
    }

    private string BuildLine(IReadOnlyDictionary<string, object?> row)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < _columns.Count; i++)
        {
            if (i > 0) sb.Append(_options.Separator);
            var raw = row.TryGetValue(_columns[i], out var v) ? v : null;
            sb.Append(QuoteIfNeeded(Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty));
        }

        return sb.ToString();
    }

    private string QuoteIfNeeded(string value)
    {
        var quote = _options.QuoteChar;
        var needsQuote = value.Contains(_options.Separator, StringComparison.Ordinal)
            || value.Contains(quote)
            || value.Contains('\n')
            || value.Contains('\r');
        if (!needsQuote) return value;

        var doubled = value.Replace(quote.ToString(), string.Concat(quote, quote));
        return string.Concat(quote, doubled, quote);
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
