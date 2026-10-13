using System;
using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.DataSource;
using Fdw.Services.Etl;
using Fdw.Services.Settings;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Factory for creating batch copy pipeline instances.
/// </summary>
public sealed class BatchCopyPipelineFactory : IBatchCopyPipelineFactory
{
    private readonly ILogger<BatchCopyPipelineFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDataGatewayProvider? _dataGateways;
    private readonly IConnectionProvider? _connectionProvider;
    private readonly IDataStoreProvider? _dataStoreProvider;
    private readonly IDataSetConfigurationProvider? _dataSets;
    private readonly IConnectionConfigurationProvider? _connectionConfigurations;
    private readonly ITransformationConfigurationProvider? _transformations;
    private readonly IRuleNotifier? _ruleNotifier;
    private readonly IEffectiveSettingsProvider? _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCopyPipelineFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating pipeline loggers.</param>
    /// <param name="dataGateways">The data gateway for pipeline execution (optional for backward compatibility).</param>
    /// <param name="connectionProvider">The connection provider for feature-detecting write capabilities (optional), injected lazily.</param>
    /// <param name="dataStoreProvider">The data store provider for resolving container metadata in the HTTP record writer path (optional).</param>
    /// <param name="dataSets">The data set configuration provider that resolves the destination and source of load steps (optional; required to run load steps).</param>
    /// <param name="connectionConfigurations">The connection configuration provider that finds a connection's type for the load capability check (optional; required to run load steps).</param>
    /// <param name="transformations">The Transformation provider that resolves the named Transformations a field mapping's chain applies when a run starts (optional; required to run a pipeline whose mappings state a chain).</param>
    /// <param name="ruleNotifier">Sends a Notify stage's notification through its rule (optional; required to run a pipeline that has a Notify stage).</param>
    /// <param name="settings">The settings provider the calculation limits of a Compute stage are read from when a run starts (optional; required to run a pipeline that has a Compute stage).</param>
    public BatchCopyPipelineFactory(
        ILogger<BatchCopyPipelineFactory> logger,
        ILoggerFactory loggerFactory,
        IDataGatewayProvider? dataGateways = null,
        IConnectionProvider? connectionProvider = null,
        IDataStoreProvider? dataStoreProvider = null,
        IDataSetConfigurationProvider? dataSets = null,
        IConnectionConfigurationProvider? connectionConfigurations = null,
        ITransformationConfigurationProvider? transformations = null,
        IRuleNotifier? ruleNotifier = null,
        IEffectiveSettingsProvider? settings = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dataGateways = dataGateways;
        _connectionProvider = connectionProvider;
        _dataStoreProvider = dataStoreProvider;
        _dataSets = dataSets;
        _connectionConfigurations = connectionConfigurations;
        _transformations = transformations;
        _ruleNotifier = ruleNotifier;
        _settings = settings;
    }

    /// <inheritdoc />
    public IGenericResult<IEtlPipeline> Create(BatchCopyPipelineConfiguration configuration)
    {
        try
        {
            EtlLog.CreatingPipelineWithFactory(_logger, configuration.Name, nameof(BatchCopyPipelineFactory));

            var pipelineLogger = _loggerFactory.CreateLogger<BatchCopyPipeline>();
            var pipeline = new BatchCopyPipeline(
                configuration,
                pipelineLogger,
                _dataGateways,
                calculationEngine: null,
                _connectionProvider,
                _dataStoreProvider,
                dataSets: _dataSets,
                connectionTypes: new RegisteredConnectionTypeResolver(_connectionConfigurations, pipelineLogger),
                transformations: _transformations,
                ruleNotifier: _ruleNotifier,
                settings: _settings);

            EtlLog.PipelineConfigurationLoaded(_logger, configuration.Name, "BatchCopy");
            return GenericResult<IEtlPipeline>.Success(pipeline);
        }
        catch (Exception ex)
        {
            return GenericResult<IEtlPipeline>.Failure(
                EtlLog.PipelineCreationFailed(_logger, configuration.Name, ex.Message));
        }
    }

    /// <inheritdoc />
    public IGenericResult<IEtlPipeline> Create(IGenericConfiguration configuration)
    {
        if (configuration == null)
        {
            return GenericResult<IEtlPipeline>.Failure(
                EtlLog.PipelineCreationFailed(_logger, "unknown", "Configuration is null"));
        }

        // A domain read is already dispatched to the implementation, so what arrives IS the engine
        // row -- there is no header to unwrap and no second body hanging off it.
        if (configuration is BatchCopyPipelineConfiguration batchConfig)
        {
            ResolveKinds(batchConfig);
            return Create(batchConfig);
        }

        return GenericResult<IEtlPipeline>.Failure(
            EtlLog.PipelineCreationFailed(_logger, "unknown",
                $"Invalid configuration type. Expected BatchCopyPipelineConfiguration, got {configuration.GetType().Name}"));
    }

    private void ResolveKinds(BatchCopyPipelineConfiguration batch)
    {
        var hasSourceDataSet = !string.IsNullOrWhiteSpace(batch.SourceDataSet);
        // A data store source is the store; its path and container are checked by ValidateSourceKind, so a
        // half-addressed one is refused there with the missing part named (FDW-1103).
        var hasSourceDataStore = !string.IsNullOrWhiteSpace(batch.SourceDataStoreName);

        if (hasSourceDataSet && hasSourceDataStore)
        {
            EtlLog.PipelineCreationFailed(_logger, batch.Name,
                "Both SourceDataSet and SourceDataStoreName are set; exactly one is required");
        }
        else if (hasSourceDataSet)
        {
            batch.SourceKind = DataSourceKinds.ByName("DataSet");
        }
        else if (hasSourceDataStore)
        {
            batch.SourceKind = DataSourceKinds.ByName("DataStore");
        }

        var hasDestDataSet = !string.IsNullOrWhiteSpace(batch.DestinationDataSet);
        // A data store destination is the store; its path and container are checked by ValidateDestinationKind, so a
        // half-addressed one is refused there with the missing part named (FDW-1269).
        var hasDestDataStore = !string.IsNullOrWhiteSpace(batch.DestinationDataStoreName);

        if (hasDestDataSet && hasDestDataStore)
        {
            EtlLog.PipelineCreationFailed(_logger, batch.Name,
                "Both DestinationDataSet and DestinationDataStoreName are set; exactly one is required");
        }
        else if (hasDestDataSet)
        {
            batch.DestinationKind = DataDestinationKinds.ByName("DataSet");
        }
        else if (hasDestDataStore)
        {
            batch.DestinationKind = DataDestinationKinds.ByName("DataStore");
        }
    }

    /// <inheritdoc />
    public IGenericResult<T> Create<T>(IGenericConfiguration configuration) where T : IGenericService
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToNewResult<T>();
        }

        if (result.Value is T typedResult)
        {
            return GenericResult<T>.Success(typedResult);
        }

        return GenericResult<T>.Failure(
            EtlLog.PipelineCreationFailed(_logger, "unknown", $"Created pipeline is not of expected type {typeof(T).Name}"));
    }

    /// <inheritdoc />
    IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
    {
        var result = Create(configuration);
        if (!result.IsSuccess || result.Value == null)
        {
            return result.ToNewResult<IGenericService>();
        }

        return GenericResult<IGenericService>.Success(result.Value);
    }
}
