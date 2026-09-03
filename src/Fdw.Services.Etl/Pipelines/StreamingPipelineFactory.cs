using System;
using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Pipelines;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Factory for creating streaming pipeline instances.
/// </summary>
public sealed class StreamingPipelineFactory : IStreamingPipelineFactory
{
    private readonly ILogger<StreamingPipelineFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDataGateway? _dataGateway;
    private readonly IConnectionProvider? _connectionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingPipelineFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating pipeline loggers.</param>
    /// <param name="dataGateway">The data gateway for pipeline execution (optional for backward compatibility).</param>
    /// <param name="connectionProvider">The connection provider for transforms (optional), injected lazily.</param>
    public StreamingPipelineFactory(
        ILogger<StreamingPipelineFactory> logger,
        ILoggerFactory loggerFactory,
        IDataGateway? dataGateway = null,
        IConnectionProvider? connectionProvider = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dataGateway = dataGateway;
        _connectionProvider = connectionProvider;
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
                _dataGateway,
                calculationEngine: null,
                _connectionProvider);

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

        var engine = UnwrapEngineBody(configuration);
        if (engine is StreamingPipelineConfiguration streamingConfig)
        {
            return Create(streamingConfig);
        }

        return GenericResult<IEtlPipeline>.Failure(
            EtlLog.PipelineCreationFailed(_logger, "unknown",
                $"Invalid configuration type. Expected StreamingPipelineConfiguration, got {configuration.GetType().Name}"));
    }

    private IGenericConfiguration UnwrapEngineBody(IGenericConfiguration configuration)
    {
        var kind = configuration switch
        {
            PipelineConfiguration { Configuration: EtlPipelineConfiguration k } => k,
            EtlPipelineConfiguration k => k,
            _ => null
        };

        if (kind?.Configuration is not { } engine)
            return configuration;

        if (engine is StreamingPipelineConfiguration streaming
            && (streaming.Transforms is null || streaming.Transforms.Count == 0)
            && kind.Transforms is { Count: > 0 })
        {
            streaming.Transforms = kind.Transforms;
            EtlLog.TransformsTransferredKindToEngine(_logger, streaming.Name, kind.Transforms.Count);
        }

        return engine;
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
