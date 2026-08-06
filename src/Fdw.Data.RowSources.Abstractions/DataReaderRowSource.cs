using System;
using System.Collections.Generic;
using System.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.DataReader.Abstractions;

/// <summary>
/// Adapts IDataReader to the IRowSourceReader interface.
/// Provides zero-allocation row access for ADO.NET data sources.
/// </summary>
/// <remarks>
/// This adapter does NOT own the underlying data reader - the caller
/// is responsible for disposal. Use within a using block for the reader.
/// </remarks>
public sealed class DataReaderRowSource : IRowSourceReader
{
    private readonly IDataReader _reader;
    private readonly Dictionary<string, int> _ordinalCache;
    private bool _hasCurrentRow;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataReaderRowSource"/> class.
    /// </summary>
    /// <param name="reader">The data reader to adapt.</param>
    public DataReaderRowSource(IDataReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _ordinalCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Pre-populate ordinal cache from schema
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (!_ordinalCache.ContainsKey(name))
            {
                _ordinalCache[name] = i;
            }
        }
    }

    /// <inheritdoc />
    public bool HasCurrentRow => _hasCurrentRow;

    /// <inheritdoc />
    public int FieldCount => _reader.FieldCount;

    /// <inheritdoc />
    public bool CanReset => false; // Forward-only readers don't support reset

    /// <inheritdoc />
    public int EstimatedAllocationsPerRow => 0; // Zero allocations per row

    /// <inheritdoc />
    public string GetFieldName(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _reader.FieldCount)
        {
            return string.Empty;
        }

        return _reader.GetName(ordinal);
    }

    /// <inheritdoc />
    public int GetFieldOrdinal(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return -1;
        }

        if (_ordinalCache.TryGetValue(fieldName, out var ordinal))
        {
            return ordinal;
        }

        // Not found in cache
        return -1;
    }

    /// <inheritdoc />
    public bool IsNull(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _reader.FieldCount)
        {
            return true;
        }

        return _reader.IsDBNull(ordinal);
    }

    /// <inheritdoc />
    public object? GetValue(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _reader.FieldCount)
        {
            return null;
        }

        if (_reader.IsDBNull(ordinal))
        {
            return null;
        }

        return _reader.GetValue(ordinal);
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
        _hasCurrentRow = _reader.Read();
        return _hasCurrentRow;
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Forward-only readers don't support reset
        // This is a no-op rather than throwing to allow generic code paths
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ordinalCache.Clear();

        // Note: We don't dispose the reader - caller owns it
    }
}
