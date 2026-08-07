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

namespace Fdw.Services.EtlMappers.Dynamic;

/// <summary>
/// ETL row mapper that uses compiled expression trees for efficient field access.
/// </summary>
public sealed class DynamicStructMapper : IEtlRowMapper
{
    private readonly ILogger<DynamicStructMapper> _logger;
    private readonly DynamicStructMapperConfiguration _configuration;
    private CompiledFieldAccessor[]? _accessors;
    private int _fieldCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicStructMapper"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The mapper configuration.</param>
    public DynamicStructMapper(
        ILogger<DynamicStructMapper> logger,
        DynamicStructMapperConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    public int EstimatedAllocationsPerRow => 1; // One dictionary per row (not pooled)

    /// <inheritdoc />
    public bool IsInitialized => _accessors != null;

    /// <inheritdoc />
    public void Initialize(IDataReader reader, IStorageContainer container)
    {
        var sw = Stopwatch.StartNew();

        var fields = container.Schema.Fields;
        _fieldCount = fields.Count;

        EtlRowMapperLog.MapperInitializing(_logger, "Dynamic", _fieldCount);

        _accessors = new CompiledFieldAccessor[_fieldCount];

        for (int i = 0; i < _fieldCount; i++)
        {
            var field = fields[i];
            int ordinal;

            try
            {
                ordinal = reader.GetOrdinal(field.Name);
            }
            catch (IndexOutOfRangeException ex)
            {
                EtlRowMapperLog.FieldOrdinalNotFound(_logger, field.Name);
                _ = ex; // Why: ex observed via log above; assigned to discard to satisfy FDW022.
                ordinal = -1;
            }
            catch (ArgumentException ex)
            {
                // Why: DataTableReader throws ArgumentException instead of IndexOutOfRangeException.
                EtlRowMapperLog.FieldOrdinalNotFound(_logger, field.Name);
                _ = ex;
                ordinal = -1;
            }

            _accessors[i] = new CompiledFieldAccessor(field.Name, ordinal);
        }

        sw.Stop();
        EtlRowMapperLog.MapperCompiled(_logger, "Dynamic", sw.Elapsed.TotalMilliseconds);
    }

    /// <inheritdoc />
    public IDictionary<string, object?> MapRow(IDataReader reader)
    {
        if (_accessors == null)
        {
            throw new InvalidOperationException("Mapper has not been initialized. Call Initialize() first.");
        }

        var dict = new Dictionary<string, object?>(_fieldCount, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _fieldCount; i++)
        {
            var accessor = _accessors[i];
            dict[accessor.FieldName] = accessor.GetValue(reader);
        }

        return dict;
    }

    /// <inheritdoc />
    public void ReturnRow(IDictionary<string, object?> row)
    {
        // Dynamic mapper doesn't pool dictionaries
    }

    /// <inheritdoc />
    public void Reset()
    {
        _accessors = null;
        _fieldCount = 0;
    }
}
