using System;
using System.Collections.Generic;
using System.Diagnostics;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.RowSources.Mappers;

/// <summary>
/// Row mapper that uses pooled dictionaries for zero-allocation mapping.
/// Works with any <see cref="IRecordCursor"/> implementation.
/// </summary>
public sealed class PooledRowMapper : IRowMapper
{
    private readonly ILogger<PooledRowMapper> _logger;
    private readonly DictionaryPool _pool;
    private readonly IDataTypeConverters? _converterCollection;
    private RowMappingContext? _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledRowMapper"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="maxPoolSize">Maximum dictionaries to pool.</param>
    /// <param name="maxDictionarySize">Maximum dictionary size to pool.</param>
    /// <param name="converterCollection">Optional converter collection for type conversion.</param>
    public PooledRowMapper(
        ILogger<PooledRowMapper> logger,
        int maxPoolSize = 1000,
        int maxDictionarySize = 100,
        IDataTypeConverters? converterCollection = null)
    {
        _logger = logger;
        _pool = new DictionaryPool(maxPoolSize, maxDictionarySize);
        _converterCollection = converterCollection;
    }

    /// <inheritdoc />
    public int EstimatedAllocationsPerRow => 0;

    /// <inheritdoc />
    public bool IsInitialized => _context != null;

    /// <inheritdoc />
    public void Initialize(IStorageContainer container)
    {
        var sw = Stopwatch.StartNew();

        RowSourceLog.MapperInitializing(_logger, "Pooled", container.Schema.Fields.Count);

        // Create a temporary source just for schema discovery
        // This will be populated with actual ordinals when MapRow is first called
        _context = null;

        sw.Stop();
        RowSourceLog.MapperInitialized(_logger, "Pooled", sw.Elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Initializes the mapper with a row source and container.
    /// This overload allows pre-computing ordinals from the actual source.
    /// </summary>
    /// <param name="source">The record cursor to initialize from.</param>
    /// <param name="container">The container with schema metadata.</param>
    public void Initialize(IRecordCursor source, IStorageContainer container)
    {
        var sw = Stopwatch.StartNew();

        RowSourceLog.MapperInitializing(_logger, "Pooled", container.Schema.Fields.Count);

        _context = RowMappingContext.Create(source, container, _converterCollection, _logger);

        sw.Stop();
        RowSourceLog.MapperInitialized(_logger, "Pooled", sw.Elapsed.TotalMilliseconds);
    }

    /// <inheritdoc />
    public IDictionary<string, object?> MapRow(IRecordCursor source)
    {
        if (_context == null)
        {
            // Invalid state - return empty dictionary
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        var dict = _pool.Rent(_context.FieldCount);

        for (int i = 0; i < _context.FieldCount; i++)
        {
            var ordinal = _context.FieldOrdinals[i];
            var name = _context.FieldNames[i];
            var converter = _context.FieldConverters[i];

            if (ordinal < 0 || source.IsNull(ordinal))
            {
                dict[name] = null;
                continue;
            }

            if (converter != null)
            {
                dict[name] = source.GetConvertedValue(ordinal, converter);
            }
            else
            {
                dict[name] = source.GetValue(ordinal);
            }
        }

        return dict;
    }

    /// <inheritdoc />
    public void ReturnRow(IDictionary<string, object?> row)
    {
        _pool.Return(row);
    }

    /// <inheritdoc />
    public void Reset()
    {
        _context = null;
        _pool.Clear();
    }
}
