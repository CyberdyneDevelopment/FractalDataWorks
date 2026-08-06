using System;
using System.Collections.Generic;
using System.IO;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using RecordParser.Extensions;

namespace Fdw.Data.RowSources.FixedWidth.Abstractions;

/// <summary>
/// Streaming fixed-width (fixed-length) row source backed by RecordParser's fixed-length raw reader.
/// Produces flat name→value rows by slicing each line per the configured
/// <see cref="FixedWidthRowSourceOptions.Fields"/>.
/// </summary>
/// <remarks>
/// Uses <see cref="FixedLengthReaderExtensions.ReadRecords(TextReader)"/>, which yields each raw
/// record line as a <c>ReadOnlyMemory&lt;char&gt;</c>. Each field is sliced by its
/// <see cref="FixedWidthField.StartIndex"/> and <see cref="FixedWidthField.Length"/> — the inverse
/// of <see cref="FixedWidthStreamRowWriter"/>'s layout.
/// </remarks>
public sealed class FixedWidthStreamRowSource : IRowSourceReader
{
    private readonly TextReader _reader;
    private readonly FixedWidthRowSourceOptions _options;
    private readonly List<string> _fieldNames;
    private readonly Dictionary<string, int> _fieldOrdinals;
    private IEnumerator<ReadOnlyMemory<char>>? _enumerator;
    private Dictionary<string, object?>? _current;
    private bool _headerSkipped;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedWidthStreamRowSource"/> class.
    /// </summary>
    /// <param name="content">The input stream containing fixed-width data.</param>
    /// <param name="options">Fixed-width processing options (field definitions are required).</param>
    /// <exception cref="ArgumentException">Thrown when no field definitions are supplied.</exception>
    public FixedWidthStreamRowSource(Stream content, FixedWidthRowSourceOptions? options = null)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        _options = options ?? new FixedWidthRowSourceOptions();
        if (_options.Fields.Count == 0)
        {
            // Why: NO FALLBACKS — a fixed-width reader cannot slice columns without offsets/widths.
            throw new ArgumentException(
                "FixedWidthRowSourceOptions.Fields must contain at least one field definition.", nameof(options));
        }

        _reader = new StreamReader(content);
        _fieldNames = [];
        _fieldOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _options.Fields.Count; i++)
        {
            _fieldNames.Add(_options.Fields[i].Name);
            _fieldOrdinals[_options.Fields[i].Name] = i;
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
        _enumerator ??= FixedLengthReaderExtensions.ReadRecords(_reader).GetEnumerator();

        if (_options.HasHeader && !_headerSkipped)
        {
            _headerSkipped = true;
            if (!_enumerator.MoveNext())
            {
                _current = null;
                return false;
            }
        }

        if (!_enumerator.MoveNext())
        {
            _current = null;
            return false;
        }

        _current = SliceLine(_enumerator.Current.Span);
        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Forward-only — no reset over a TextReader-backed pipeline.
    }

    private Dictionary<string, object?> SliceLine(ReadOnlySpan<char> line)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in _options.Fields)
        {
            if (field.StartIndex >= line.Length)
            {
                row[field.Name] = _options.Trim ? string.Empty : null;
                continue;
            }

            var available = Math.Min(field.Length, line.Length - field.StartIndex);
            var slice = line.Slice(field.StartIndex, available);
            var value = _options.Trim ? slice.Trim(field.PaddingChar).ToString() : slice.ToString();
            row[field.Name] = value;
        }

        return row;
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
