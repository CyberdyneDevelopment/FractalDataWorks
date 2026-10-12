using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using RecordParser.Extensions;

namespace Fdw.Data.RowSources.Delimited.Abstractions;

/// <summary>
/// Streaming delimited (CSV / variable-length) row source backed by RecordParser's raw reader.
/// Produces flat name→value rows using the column names supplied in
/// <see cref="DelimitedRowSourceOptions.Columns"/>.
/// </summary>
/// <remarks>
/// Uses <see cref="VariableLengthReaderRawExtensions.ReadRecordsRaw{T}"/>, which yields positional
/// columns at runtime (no compile-time POCO). Each line is projected into a
/// <c>Dictionary&lt;string, object?&gt;</c> keyed by the configured column names — the same dynamic
/// row shape the JSON/XML sources produce.
/// </remarks>
public sealed class DelimitedStreamRowSource : IRowSourceReader
{
    private readonly TextReader _reader;
    private readonly DelimitedRowSourceOptions _options;
    private readonly List<string> _fieldNames;
    private readonly Dictionary<string, int> _fieldOrdinals;
    private IEnumerator<Dictionary<string, object?>>? _enumerator;
    private Dictionary<string, object?>? _current;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedStreamRowSource"/> class.
    /// </summary>
    /// <param name="content">The input stream containing delimited data.</param>
    /// <param name="options">Delimited processing options (column names are required).</param>
    /// <exception cref="ArgumentException">Thrown when no column names are supplied.</exception>
    public DelimitedStreamRowSource(Stream content, DelimitedRowSourceOptions? options = null)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        _options = options ?? new DelimitedRowSourceOptions();
        if (_options.Columns.Count == 0)
        {
            throw new ArgumentException(
                "DelimitedRowSourceOptions.Columns must contain at least one column name.", nameof(options));
        }

        _reader = new StreamReader(content);
        _fieldNames = [.. _options.Columns];
        _fieldOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _fieldNames.Count; i++)
        {
            _fieldOrdinals[_fieldNames[i]] = i;
        }
    }

    /// <inheritdoc />
    public bool HasCurrentRow => _current is not null;

    /// <inheritdoc />
    public int FieldCount => _fieldNames.Count;

    /// <inheritdoc />
    public bool CanReset => false;

    /// <inheritdoc />
    public int EstimatedAllocationsPerRow => 1;

    /// <inheritdoc />
    public string GetFieldName(int ordinal)
        => ordinal < 0 || ordinal >= _fieldNames.Count ? string.Empty : _fieldNames[ordinal];

    /// <inheritdoc />
    public int GetFieldOrdinal(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName)) return -1;
        return _fieldOrdinals.TryGetValue(fieldName, out var ordinal) ? ordinal : -1;
    }

    /// <inheritdoc />
    public bool IsNull(int ordinal)
    {
        if (_current is null || ordinal < 0 || ordinal >= _fieldNames.Count) return true;
        return !_current.TryGetValue(_fieldNames[ordinal], out var value) || value is null;
    }

    /// <inheritdoc />
    public object? GetValue(int ordinal)
    {
        if (_current is null || ordinal < 0 || ordinal >= _fieldNames.Count) return null;
        return _current.TryGetValue(_fieldNames[ordinal], out var value) ? value : null;
    }

    /// <inheritdoc />
    public object? GetConvertedValue(int ordinal, IDataTypeConverter converter)
    {
        var rawValue = GetValue(ordinal);
        return rawValue is null ? null : converter.ToClr(rawValue);
    }

    /// <inheritdoc />
    public bool Read()
    {
        _enumerator ??= BuildEnumerator();
        if (_enumerator.MoveNext())
        {
            _current = _enumerator.Current;
            return true;
        }

        _current = null;
        return false;
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Forward-only — no reset over a TextReader-backed pipeline.
    }

    private IEnumerator<Dictionary<string, object?>> BuildEnumerator()
    {
        var rawOptions = new VariableLengthReaderRawOptions
        {
            ColumnCount = _fieldNames.Count,
            Separator = _options.Separator,
            HasHeader = _options.HasHeader,
            ContainsQuotedFields = _options.ContainsQuotedFields,
            Trim = _options.Trim
        };

        var rows = VariableLengthReaderRawExtensions.ReadRecordsRaw(_reader, rawOptions, getColumn =>
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < _fieldNames.Count; i++)
            {
                row[_fieldNames[i]] = getColumn(i);
            }

            return row;
        });

        return rows.GetEnumerator();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _enumerator?.Dispose();
        _reader.Dispose();
        _current = null;
    }
}
