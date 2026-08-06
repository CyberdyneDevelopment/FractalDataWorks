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
using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.DataSource;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Factory for creating batch copy pipeline instances.
/// </summary>
public sealed class BatchCopyPipelineFactory : IBatchCopyPipelineFactory
{
    private readonly ILogger<BatchCopyPipelineFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDataGateway? _dataGateway;
    // Why: Lazy so the factory stays pure (FDW045). Cross-collection connection provider, used only at
    // Create() time; Lazy defers resolution past construction (see StreamingPipelineFactory).
    private readonly Lazy<IFdwServiceProvider<IGenericConnection, IGenericConfiguration>>? _connectionProvider;
    private readonly IDataStoreProvider? _dataStoreProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCopyPipelineFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for factory operations.</param>
    /// <param name="loggerFactory">The logger factory for creating pipeline loggers.</param>
    /// <param name="dataGateway">The data gateway for pipeline execution (optional for backward compatibility).</param>
    /// <param name="connectionProvider">The connection provider for feature-detecting write capabilities (optional), injected lazily.</param>
    /// <param name="dataStoreProvider">The data store provider for resolving container metadata in the HTTP record writer path (optional).</param>
    public BatchCopyPipelineFactory(
        ILogger<BatchCopyPipelineFactory> logger,
        ILoggerFactory loggerFactory,
        IDataGateway? dataGateway = null,
        Lazy<IFdwServiceProvider<IGenericConnection, IGenericConfiguration>>? connectionProvider = null,
        IDataStoreProvider? dataStoreProvider = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _dataGateway = dataGateway;
        _connectionProvider = connectionProvider;
        _dataStoreProvider = dataStoreProvider;
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
                _dataGateway,
                calculationEngine: null,
                _connectionProvider?.Value,
                _dataStoreProvider);

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

        // Why: the runtime service provider composes and hands the ROOT header
        // (PipelineConfiguration → .Configuration EtlPipelineConfiguration → .Configuration
        // BatchCopyPipelineConfiguration). Unwrap to the engine body the pipeline consumes.
        // A bare engine body (test/system-context caller) is accepted directly.
        var engine = UnwrapEngineBody(configuration);
        if (engine is BatchCopyPipelineConfiguration batchConfig)
        {
            return Create(batchConfig);
        }

        return GenericResult<IEtlPipeline>.Failure(
            EtlLog.PipelineCreationFailed(_logger, "unknown",
                $"Invalid configuration type. Expected BatchCopyPipelineConfiguration, got {configuration.GetType().Name}"));
    }

    // Why: peel the typed-body chain by declared marker properties — header.Configuration is the
    // ETL-kind body (EtlPipelineConfiguration), whose .Configuration is the engine body. Returns the
    // deepest non-null typed body, or the input when it is already an engine body.
    // Transforms are a KIND-level child collection (FK EtlPipelineId) composed onto
    // EtlPipelineConfiguration; the engine body exposes a [NotMapped] Transforms the runtime pipeline
    // reads. Carry the composed transforms across the kind→engine seam (the engine cannot load them via
    // its own FK), else the pipeline runs with "0 transforms" and the Map step is a silent passthrough.
    // SourceKind / DestinationKind are also [NotMapped] discriminators populated here from whichever
    // of the two mutually-exclusive fields (DataSet vs ConnectionName) is non-empty. Fail loud if both
    // or neither are set — no fallback, no guess.
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

        if (engine is BatchCopyPipelineConfiguration batch)
        {
            if ((batch.Transforms is null || batch.Transforms.Count == 0)
                && kind.Transforms is { Count: > 0 })
            {
                batch.Transforms = kind.Transforms;
                EtlLog.TransformsTransferredKindToEngine(_logger, batch.Name, kind.Transforms.Count);
            }

            // Why: SourceKind and DestinationKind are runtime discriminators; they are NOT persisted
            // as DB columns ([NotMapped]). The factory is the authoritative place to resolve them from
            // the mutually-exclusive configuration fields (SourceDataSet vs SourceConnectionName).
            // Any other logic that needs to branch on Kind can rely on these being set here.
            ResolveKinds(batch);
        }

        return engine;
    }

    // Why: separated from UnwrapEngineBody so the logic is readable and independently testable.
    // No return value — mutates the [NotMapped] Kind properties on the configuration object
    // (they are intentionally not persisted; this factory is the single writer).
    private void ResolveKinds(BatchCopyPipelineConfiguration batch)
    {
        var hasSourceDataSet = !string.IsNullOrWhiteSpace(batch.SourceDataSet);
        var hasSourceConnection = !string.IsNullOrWhiteSpace(batch.SourceConnectionName);

        if (hasSourceDataSet && hasSourceConnection)
        {
            // Why: both fields being populated is a configuration authoring error — the factory
            // cannot guess which the caller intended. Fail loud: leave Kind null so the executor
            // surfaces a SourceKindRequired failure rather than silently picking one.
            EtlLog.PipelineCreationFailed(_logger, batch.Name,
                "Both SourceDataSet and SourceConnectionName are set; exactly one is required");
        }
        else if (hasSourceDataSet)
        {
            batch.SourceKind = DataSourceKinds.ByName("DataSet");
        }
        else if (hasSourceConnection)
        {
            batch.SourceKind = DataSourceKinds.ByName("Connection");
        }
        // Why: if neither is set, leave SourceKind null — the executor's SourceKindRequired check
        // surfaces the failure with the pipeline name in context (better error locality).

        var hasDestDataSet = !string.IsNullOrWhiteSpace(batch.DestinationDataSet);
        var hasDestConnection = !string.IsNullOrWhiteSpace(batch.DestinationConnectionName);

        if (hasDestDataSet && hasDestConnection)
        {
            EtlLog.PipelineCreationFailed(_logger, batch.Name,
                "Both DestinationDataSet and DestinationConnectionName are set; exactly one is required");
        }
        else if (hasDestDataSet)
        {
            batch.DestinationKind = DataDestinationKinds.ByName("DataSet");
        }
        else if (hasDestConnection)
        {
            batch.DestinationKind = DataDestinationKinds.ByName("Connection");
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
