using System;
using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Etl;
using Fdw.Services.Settings;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Factory for creating streaming pipeline instances.
/// </summary>
public sealed class StreamingPipelineFactory : IStreamingPipelineFactory
{
    private readonly ILogger<StreamingPipelineFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDataGatewayProvider? _dataGateways;
    private readonly IConnectionProvider? _connectionProvider;
    private readonly ITransformationConfigurationProvider? _transformations;
    private readonly IRuleNotifier? _ruleNotifier;
    private readonly IEffectiveSettingsProvider? _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingPipelineFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating pipeline loggers.</param>
    /// <param name="dataGateways">The data gateway for pipeline execution (optional for backward compatibility).</param>
    /// <param name="connectionProvider">The connection provider for transforms (optional), injected lazily.</param>
    /// <param name="transformations">The Transformation provider that resolves the named Transformations a field mapping's chain applies when a run starts (optional; required to run a pipeline whose mappings state a chain).</param>
    /// <param name="ruleNotifier">Sends a Notify stage's notification through its rule (optional; required to run a pipeline that has a Notify stage).</param>
    /// <param name="settings">The settings provider the calculation limits of a Compute stage are read from when a run starts (optional; required to run a pipeline that has a Compute stage).</param>
    public StreamingPipelineFactory(
        ILogger<StreamingPipelineFactory> logger,
        ILoggerFactory loggerFactory,
        IDataGatewayProvider? dataGateways = null,
        IConnectionProvider? connectionProvider = null,
        ITransformationConfigurationProvider? transformations = null,
        IRuleNotifier? ruleNotifier = null,
        IEffectiveSettingsProvider? settings = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dataGateways = dataGateways;
        _connectionProvider = connectionProvider;
        _transformations = transformations;
        _ruleNotifier = ruleNotifier;
        _settings = settings;
    }

    /// <inheritdoc />
    public IGenericResult<IEtlPipeline> Create(StreamingPipelineConfiguration configuration)
    {
        try
        {
            EtlLog.CreatingPipelineWithFactory(_logger, configuration.Name, nameof(StreamingPipelineFactory));

            var pipelineLogger = _loggerFactory.CreateLogger<StreamingPipeline>();
            var pipeline = new StreamingPipeline(
                configuration,
                pipelineLogger,
                _dataGateways,
                calculationEngine: null,
                _connectionProvider,
                transformations: _transformations,
                ruleNotifier: _ruleNotifier,
                settings: _settings);

            EtlLog.PipelineConfigurationLoaded(_logger, configuration.Name, "Streaming");
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

        if (configuration is StreamingPipelineConfiguration streamingConfig)
        {
            return Create(streamingConfig);
        }

        return GenericResult<IEtlPipeline>.Failure(
            EtlLog.PipelineCreationFailed(_logger, "unknown",
                $"Invalid configuration type. Expected StreamingPipelineConfiguration, got {configuration.GetType().Name}"));
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
