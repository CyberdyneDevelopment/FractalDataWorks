using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Fdw.Conventions;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Xml.Abstractions;

/// <summary>
/// Streaming XML row source that reads row elements without loading entire document.
/// Supports both sync and async reading patterns.
/// </summary>
/// <remarks>
/// Uses XmlReader for forward-only streaming - memory efficient for large documents.
/// Security settings are applied to prevent XXE and billion laughs attacks.
/// </remarks>
public sealed class XmlStreamRowSource : IRowSourceReader, IAsyncRowSourceReader
{
    private readonly XmlReader _reader;
    private readonly XmlRowSourceOptions _options;
    private readonly Dictionary<string, int> _fieldOrdinals;
    private readonly List<string> _fieldNames;
    private readonly Dictionary<string, object?> _currentRowValues;
    private bool _hasCurrentRow;
    private bool _disposed;
    private int _depth;

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlStreamRowSource"/> class.
    /// </summary>
    /// <param name="stream">The input stream containing XML data.</param>
    /// <param name="options">XML processing options.</param>
    public XmlStreamRowSource(Stream stream, XmlRowSourceOptions? options = null)
    {
        _options = options ?? new XmlRowSourceOptions();
        var settings = _options.CreateSecureSettings();
        _reader = XmlReader.Create(stream, settings);
        _fieldOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _fieldNames = [];
        _currentRowValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlStreamRowSource"/> class.
    /// </summary>
    /// <param name="reader">An existing XmlReader (caller must have applied security settings).</param>
    /// <param name="options">XML processing options.</param>
    public XmlStreamRowSource(XmlReader reader, XmlRowSourceOptions? options = null)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _options = options ?? new XmlRowSourceOptions();
        _fieldOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        _fieldNames = [];
        _currentRowValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool HasCurrentRow => _hasCurrentRow;

    /// <inheritdoc />
    public int FieldCount => _fieldNames.Count;

    /// <inheritdoc />
    public bool CanReset => false; // Forward-only streaming

    /// <inheritdoc />
    public int EstimatedAllocationsPerRow => 1; // Dictionary per row

    /// <inheritdoc />
    public string GetFieldName(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _fieldNames.Count)
        {
            return string.Empty;
        }

        return _fieldNames[ordinal];
    }

    /// <inheritdoc />
    public int GetFieldOrdinal(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return -1;
        }

        return _fieldOrdinals.TryGetValue(fieldName, out var ordinal) ? ordinal : -1;
    }

    /// <inheritdoc />
    public bool IsNull(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _fieldNames.Count)
        {
            return true;
        }

        var fieldName = _fieldNames[ordinal];
        return !_currentRowValues.TryGetValue(fieldName, out var value) || value == null;
    }

    /// <inheritdoc />
    public object? GetValue(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _fieldNames.Count)
        {
            return null;
        }

        var fieldName = _fieldNames[ordinal];
        return _currentRowValues.TryGetValue(fieldName, out var value) ? value : null;
    }

    /// <inheritdoc />
    public object? GetConvertedValue(int ordinal, IDataTypeConverter converter)
    {
        var rawValue = GetValue(ordinal);
        if (rawValue == null)
        {
            return null;
        }

        return converter.ToClr(rawValue);
    }

    /// <inheritdoc />
    public bool Read()
    {
        return ReadRowElement();
    }

    /// <inheritdoc />
    public ValueTask<bool> Read(CancellationToken cancellationToken = default)
    {
        // XmlReader.ReadAsync is available but for simplicity we use sync
        // For true async, would need XmlReader.Create with async stream
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<bool>(ReadRowElement());
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Forward-only, no reset supported
    }

    private bool ReadRowElement()
    {
        _currentRowValues.Clear();
        _hasCurrentRow = false;

        while (_reader.Read())
        {
            // Check depth limit
            if (_reader.Depth > _options.MaxDepth)
            {
                continue;
            }

            if (_reader.NodeType == XmlNodeType.Element && IsRowElement(_reader.LocalName))
            {
                _depth = _reader.Depth;
                ReadRowContent();
                _hasCurrentRow = true;
                return true;
            }
        }

        return false;
    }

    private bool IsRowElement(string localName)
    {
        if (!string.IsNullOrEmpty(_options.RowElementName))
        {
            return string.Equals(localName, _options.RowElementName, StringComparison.OrdinalIgnoreCase);
        }

        // If no row element specified, treat any element at depth 1+ as potential row
        return _reader.Depth >= 1;
    }

    [ConventionOverride(MaxCyclomaticComplexity = 20)]  // XML parsing — attribute extraction, element traversal, depth tracking, content reading
    private void ReadRowContent()
    {
        // Read attributes first
        if (_options.IncludeAttributes && _reader.HasAttributes)
        {
            while (_reader.MoveToNextAttribute())
            {
                AddFieldValue(_reader.LocalName, _reader.Value);
            }

            _reader.MoveToElement();
        }

        // If element is empty, we're done
        if (_reader.IsEmptyElement)
        {
            return;
        }

        // Read child elements
        var startDepth = _reader.Depth;

        while (_reader.Read())
        {
            if (_reader.Depth <= startDepth)
            {
                break;
            }

            if (_reader.NodeType == XmlNodeType.Element && _reader.Depth == startDepth + 1)
            {
                var fieldName = _reader.LocalName;
                string? value = null;

                if (_options.UseElementContent)
                {
                    // Read element content manually to avoid ReadElementContentAsString() advancing
                    // past the EndElement tag, which would cause the loop to skip sibling elements.
                    if (!_reader.IsEmptyElement)
                    {
                        _reader.Read(); // advance to Text or EndElement
                        if (_reader.NodeType == XmlNodeType.Text)
                        {
                            value = _reader.Value;
                            _reader.Read(); // advance to EndElement so loop continues correctly
                        }
                    }
                }

                // Also check for attributes on child elements
                if (_options.IncludeAttributes && _reader.HasAttributes)
                {
                    while (_reader.MoveToNextAttribute())
                    {
                        AddFieldValue($"{fieldName}_{_reader.LocalName}", _reader.Value);
                    }

                    _reader.MoveToElement();
                }

                if (value != null)
                {
                    AddFieldValue(fieldName, value);
                }
            }
        }
    }

    private void AddFieldValue(string name, object? value)
    {
        if (!_fieldOrdinals.ContainsKey(name))
        {
            _fieldOrdinals[name] = _fieldNames.Count;
            _fieldNames.Add(name);
        }

        _currentRowValues[name] = value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _reader.Dispose();
        _currentRowValues.Clear();
        _fieldOrdinals.Clear();
        _fieldNames.Clear();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
