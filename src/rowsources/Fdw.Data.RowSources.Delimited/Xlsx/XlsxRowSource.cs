using System;
using System.Collections.Generic;
using System.IO;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>
/// Reads one worksheet of an Excel workbook (.xlsx) as rows: the counterpart of <see cref="XlsxRowWriter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Cells are read by column letter, so a row with empty cells in the middle keeps its columns; the names come from
/// <see cref="XlsxRowSourceOptions.Columns"/>, left to right from column A. A number is a long when it is whole and a
/// decimal otherwise; a cell whose number format is a date is a <see cref="DateTime"/>, and one whose format is a time is
/// a <see cref="TimeOnly"/> (a <see cref="TimeSpan"/> when it is a day or longer). Text, booleans and inline or shared
/// strings are read as they are, and an error cell as its text (#DIV/0!). A formula is read as the value Excel cached.
/// </para>
/// <para>
/// A workbook is a zip, which needs random access: a stream that cannot seek is copied into memory first, as the
/// framework does. The worksheet itself is read forward, one row at a time. It reads what the workbook holds: it does not
/// recalculate, and it does not read a password-protected or encrypted workbook (that is not a zip, and is refused).
/// </para>
/// </remarks>
public sealed class XlsxRowSource : IRowSourceReader
{
    private readonly Stream _content;
    private readonly XlsxRowSourceOptions _options;
    private readonly List<string> _fieldNames;
    private readonly Dictionary<string, int> _fieldOrdinals;
    private XlsxSheetCursor? _cursor;
    private object?[]? _current;
    private long _rowsRead;
    private bool _headerHandled;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="XlsxRowSource"/> class.</summary>
    /// <param name="content">The workbook's bytes; it is left open.</param>
    /// <param name="options">The columns and worksheet to read.</param>
    /// <exception cref="ArgumentNullException">The content or the options are missing.</exception>
    /// <exception cref="ArgumentException">No column names are supplied.</exception>
    public XlsxRowSource(Stream content, XlsxRowSourceOptions options)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.Columns.Count == 0)
        {
            throw new ArgumentException("XlsxRowSourceOptions.Columns must contain at least one column name.", nameof(options));
        }

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
        => !string.IsNullOrEmpty(fieldName) && _fieldOrdinals.TryGetValue(fieldName, out var ordinal) ? ordinal : -1;

    /// <inheritdoc />
    public bool IsNull(int ordinal) => GetValue(ordinal) is null;

    /// <inheritdoc />
    public object? GetValue(int ordinal)
        => _current is null || ordinal < 0 || ordinal >= _current.Length ? null : _current[ordinal];

    /// <inheritdoc />
    public object? GetConvertedValue(int ordinal, IDataTypeConverter converter)
    {
        var raw = GetValue(ordinal);
        return raw is null ? null : converter.ToClr(raw);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidDataException">The content is not a workbook, the worksheet cannot be told, or a cell is malformed (a part that is not well-formed XML is an <see cref="System.Xml.XmlException"/>).</exception>
    public bool Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_options.MaxRows > 0 && _rowsRead >= _options.MaxRows)
        {
            _current = null;
            return false;
        }

        _cursor ??= XlsxSheetCursor.Open(_content, _options.SheetName);
        if (_options.HasHeader && !_headerHandled)
        {
            _headerHandled = true;
            _cursor.ReadRow(new object?[_fieldNames.Count]);
        }

        var values = new object?[_fieldNames.Count];
        if (!_cursor.ReadRow(values))
        {
            _current = null;
            return false;
        }

        _current = values;
        _rowsRead++;
        return true;
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Forward-only: the worksheet is streamed from the zip and is not read twice.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cursor?.Dispose();
        _current = null;
    }
}
