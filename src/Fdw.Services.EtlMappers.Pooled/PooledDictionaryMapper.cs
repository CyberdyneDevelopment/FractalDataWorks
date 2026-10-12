using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Fdw.Data.Abstractions;
using Fdw.Services.EtlMappers.Abstractions;
using Fdw.Services.EtlMappers.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Fdw.Services.EtlMappers;
using Fdw.Services;
using Fdw;

namespace Fdw.Services.EtlMappers.Pooled;

/// <summary>
/// ETL row mapper that uses pooled dictionaries for zero-allocation mapping.
/// </summary>
public sealed class PooledDictionaryMapper : IEtlRowMapper
{
    private readonly ILogger<PooledDictionaryMapper> _logger;
    private readonly DictionaryPool _pool;
    private RowMappingContext? _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledDictionaryMapper"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The mapper configuration.</param>
    public PooledDictionaryMapper(
        ILogger<PooledDictionaryMapper> logger,
        PooledDictionaryMapperConfiguration configuration)
    {
        _logger = logger;
        _pool = new DictionaryPool(configuration.MaxPoolSize, configuration.MaxDictionarySize);
    }

    /// <inheritdoc />
    public int EstimatedAllocationsPerRow => 0;

    /// <inheritdoc />
    public bool IsInitialized => _context != null;

    /// <inheritdoc />
    public void Initialize(IDataReader reader, IStorageContainer container)
    {
        var sw = Stopwatch.StartNew();

        EtlRowMapperLog.MapperInitializing(_logger, "Pooled", container.Schema.Fields.Count);

        _context = RowMappingContext.Create(reader, container);

        sw.Stop();
        EtlRowMapperLog.MapperCompiled(_logger, "Pooled", sw.Elapsed.TotalMilliseconds);
    }

    /// <inheritdoc />
    public IDictionary<string, object?> MapRow(IDataReader reader)
    {
        if (_context == null)
        {
            throw new InvalidOperationException("Mapper has not been initialized. Call Initialize() first.");
        }

        var dict = _pool.Rent(_context.FieldCount);

        for (int i = 0; i < _context.FieldCount; i++)
        {
            var ordinal = _context.FieldOrdinals[i];
            var name = _context.FieldNames[i];

            if (ordinal < 0 || reader.IsDBNull(ordinal))
            {
                dict[name] = null;
                continue;
            }

            dict[name] = reader.GetValue(ordinal);
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
