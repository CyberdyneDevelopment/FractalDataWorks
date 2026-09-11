using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// Service for reading, persisting, and validating DataSet schema definitions.
/// </summary>
/// <remarks>
/// All ConfigurationDb access is delegated to <see cref="DataSetConfigurationProvider"/>,
/// which owns the gateway for the DataSet domain. This service contains only orchestration
/// logic (load → validate) with no direct gateway calls.
/// </remarks>
public sealed class DataSetSchemaService : IDataSetSchemaService
{
    private readonly DataSetConfigurationProvider _provider;
    private readonly ILogger<DataSetSchemaService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetSchemaService"/> class.
    /// </summary>
    /// <param name="provider">Domain configuration provider owning all DataSet gateway access.</param>
    /// <param name="logger">Optional logger instance.</param>
    public DataSetSchemaService(
        DataSetConfigurationProvider provider,
        ILogger<DataSetSchemaService>? logger = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? NullLogger<DataSetSchemaService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<DataSetFieldDefinition>>> GetSchema(
        Guid dataSetId, CancellationToken cancellationToken = default)
    {
        DataSetSchemaLog.GetSchemaStarted(_logger, dataSetId);

        // A dataset's fields are its own child rows, so they arrive with the dataset -- there is no
        // separate field read to make.
        var loaded = await _provider.Get(dataSetId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess || loaded.Value is null)
        {
            return GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Failure(
                DataSetSchemaLog.GetSchemaFailed(_logger, dataSetId,
                    loaded.CurrentMessage ?? "Provider returned failure"));
        }

        IReadOnlyList<DataSetFieldDefinition> fields =
            [.. loaded.Value.Fields.Select(f => new DataSetFieldDefinition
            {
                DataSetId = dataSetId,
                FieldName = f.Name,
                ScalarTypeName = f.TypeName,
                IsNullable = f.IsNullable,
                Ordinal = f.Ordinal,
                Description = f.Description,
            })];

        DataSetSchemaLog.GetSchemaSucceeded(_logger, dataSetId, fields.Count);
        return GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success(fields);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> SaveSchema(
        Guid dataSetId,
        IReadOnlyList<DataSetFieldDefinition> fields,
        CancellationToken cancellationToken = default)
    {
        DataSetSchemaLog.SaveSchemaStarted(_logger, dataSetId, fields.Count);

        // The fields hang from the dataset, so the write is a save of the dataset carrying them.
        var loaded = await _provider.Get(dataSetId, cancellationToken).ConfigureAwait(false);
        if (!loaded.IsSuccess || loaded.Value is null)
        {
            return GenericResult.Failure(
                DataSetSchemaLog.SaveSchemaFailed(_logger, dataSetId,
                    loaded.CurrentMessage ?? "Provider returned failure"));
        }

        var dataSet = loaded.Value;
        dataSet.Fields = [.. fields.Select(f => new DataSetFieldConfiguration
        {
            Name = f.FieldName,
            TypeName = f.ScalarTypeName,
            IsNullable = f.IsNullable,
            Ordinal = f.Ordinal,
            Description = f.Description,
        })];

        var result = await _provider
            .Save(dataSet, dataSet.Domain, dataSet.Implementation, dataSet.Name, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                DataSetSchemaLog.SaveSchemaFailed(_logger, dataSetId,
                    result.CurrentMessage ?? "Provider returned failure"));
        }

        DataSetSchemaLog.SaveSchemaSucceeded(_logger, dataSetId);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> ValidateConformance(
        Guid physicalDataSetId, Guid abstractDataSetId,
        CancellationToken cancellationToken = default)
    {
        DataSetSchemaLog.ConformanceCheckStarted(_logger, physicalDataSetId, abstractDataSetId);

        var physicalResult = await GetSchema(physicalDataSetId, cancellationToken).ConfigureAwait(false);

        if (!physicalResult.IsSuccess)
        {
            return GenericResult.Failure(
                DataSetSchemaLog.GetSchemaFailed(_logger, physicalDataSetId,
                    physicalResult.CurrentMessage ?? "Failed to load physical schema"));
        }

        var abstractResult = await GetSchema(abstractDataSetId, cancellationToken).ConfigureAwait(false);

        if (!abstractResult.IsSuccess)
        {
            return GenericResult.Failure(
                DataSetSchemaLog.GetSchemaFailed(_logger, abstractDataSetId,
                    abstractResult.CurrentMessage ?? "Failed to load abstract schema"));
        }

        var physicalFields = physicalResult.Value!;

        foreach (var abstractField in abstractResult.Value!)
        {
            var match = physicalFields.FirstOrDefault(f =>
                string.Equals(f.FieldName, abstractField.FieldName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.ScalarTypeName, abstractField.ScalarTypeName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                return GenericResult.Failure(
                    DataSetSchemaLog.ConformanceCheckFailed(_logger, physicalDataSetId, abstractField.FieldName));
            }
        }

        DataSetSchemaLog.ConformanceCheckPassed(_logger, physicalDataSetId, abstractDataSetId);
        return GenericResult.Success();
    }
}
