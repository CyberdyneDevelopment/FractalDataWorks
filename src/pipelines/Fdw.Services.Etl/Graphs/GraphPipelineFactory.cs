using System;
using Fdw.Abstractions;
using Fdw.Services.Etl.Flow;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Configuration;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.EtlGateway;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Settings;
using Fdw.Data.Transformations.Configuration;
using Fdw.Operations.Abstractions.Thresholds;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Creates <see cref="GraphPipeline"/> instances from a <see cref="GraphPipelineConfiguration"/> (FDW-1396).
/// </summary>
public sealed class GraphPipelineFactory : IGraphPipelineFactory
{
    private readonly ILogger<GraphPipelineFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFlowExecutor _flowExecutor;
    private readonly IDataGatewayProvider? _dataGateways;
    private readonly IConnectionProvider? _connectionProvider;
    private readonly IDataStoreProvider? _dataStoreProvider;
    private readonly IDataSetConfigurationProvider? _dataSets;
    private readonly IConnectionConfigurationProvider? _connectionConfigurations;
    private readonly Func<IPipelineRunStarter>? _runStarter;
    private readonly IEtlGateway? _etlGateway;
    private readonly IRuleNotifier? _ruleNotifier;
    private readonly ITransformationConfigurationProvider? _transformations;
    private readonly IEffectiveSettingsProvider? _settings;
    private readonly IExecutionTracker? _tracker;
    private readonly RunningFlows? _runningFlows;
    private readonly IThresholdEvaluator? _thresholds;
    private readonly TimeProvider? _clock;

    /// <summary>Initializes a new instance of the <see cref="GraphPipelineFactory"/> class.</summary>
    /// <param name="logger">The factory's logger.</param>
    /// <param name="loggerFactory">Makes the logger of each pipeline.</param>
    /// <param name="flowExecutor">Runs the graph.</param>
    /// <param name="dataGateways">The data gateway provider a source reads through (optional; a run refuses without it).</param>
    /// <param name="connectionProvider">The connection provider a destination's load checks capabilities with (optional).</param>
    /// <param name="dataStoreProvider">The data store provider that resolves a container destination (optional).</param>
    /// <param name="dataSets">The data set configuration provider that resolves a data set destination (optional).</param>
    /// <param name="connectionConfigurations">Finds a connection's type, so a load is checked against the capabilities the type lists (optional).</param>
    /// <param name="runStarter">Resolves the run starter an Execute Pipeline step starts its pipeline through (optional).</param>
    /// <param name="etlGateway">The ETL gateway a SQL source or Execute SQL step runs its script through (optional; a graph with either refuses to build without it).</param>
    /// <param name="ruleNotifier">What sends a notification through a rule (optional; a graph with a Notify step refuses to run without it).</param>
    /// <param name="transformations">Reads the Built transformations a Compute tool applies (optional; a graph with a Compute tool refuses to run without it).</param>
    /// <param name="settings">The effective settings the transformations are compiled under (optional, as above).</param>
    /// <param name="tracker">Records how the steps of a tracked run went as child executions of the run (optional: an untracked host passes none and nothing is recorded).</param>
    /// <param name="runningFlows">What the live view reads; each pipeline made lists its run there while it runs (optional: a host without the view passes none).</param>
    /// <param name="thresholds">Passed to each pipeline made, to judge its components' figures against thresholds (a run refuses without it, and a host that runs pipelines refuses to start without it).</param>
    /// <param name="clock">Passed to each pipeline made, as the clock its Compute stages' calculations read the time from (optional).</param>
    public GraphPipelineFactory(
        ILogger<GraphPipelineFactory> logger,
        ILoggerFactory loggerFactory,
        IFlowExecutor flowExecutor,
        IDataGatewayProvider? dataGateways = null,
        IConnectionProvider? connectionProvider = null,
        IDataStoreProvider? dataStoreProvider = null,
        IDataSetConfigurationProvider? dataSets = null,
        IConnectionConfigurationProvider? connectionConfigurations = null,
        Func<IPipelineRunStarter>? runStarter = null,
        IEtlGateway? etlGateway = null,
        IRuleNotifier? ruleNotifier = null,
        ITransformationConfigurationProvider? transformations = null,
        IEffectiveSettingsProvider? settings = null,
        IExecutionTracker? tracker = null,
        RunningFlows? runningFlows = null,
        IThresholdEvaluator? thresholds = null,
        TimeProvider? clock = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _flowExecutor = flowExecutor;
        _dataGateways = dataGateways;
        _connectionProvider = connectionProvider;
        _dataStoreProvider = dataStoreProvider;
        _dataSets = dataSets;
        _connectionConfigurations = connectionConfigurations;
        _runStarter = runStarter;
        _etlGateway = etlGateway;
        _ruleNotifier = ruleNotifier;
        _transformations = transformations;
        _settings = settings;
        _tracker = tracker;
        _runningFlows = runningFlows;
        _thresholds = thresholds;
        _clock = clock;
    }

    /// <summary>
    /// Gets how often, in seconds, a destination writes what it has committed when the settings state no interval (FDW-4957): the default the graph
    /// pipeline type declares, resolved when it registered this factory. 0 writes after every commit.
    /// </summary>
    public required int CommittedReportSeconds { get; init; }

    /// <summary>
    /// Gets how many unmatched keys a report carries when the settings state no size (FDW-4988): the default the graph pipeline type declares, resolved when
    /// it registered this factory. 0 carries only the count.
    /// </summary>
    public required int UnmatchedKeySample { get; init; }

    /// <inheritdoc />
    public IGenericResult<IEtlPipeline> Create(GraphPipelineConfiguration configuration)
    {
        try
        {
            EtlLog.CreatingPipelineWithFactory(_logger, configuration.Name, nameof(GraphPipelineFactory));

            var pipelineLogger = _loggerFactory.CreateLogger<GraphPipeline>();
            var pipeline = new GraphPipeline(
                configuration,
                pipelineLogger,
                _flowExecutor,
                _dataGateways,
                _connectionProvider,
                _dataStoreProvider,
                _dataSets,
                new RegisteredConnectionTypeResolver(_connectionConfigurations, pipelineLogger),
                _runStarter,
                _etlGateway,
                _ruleNotifier,
                _transformations,
                _settings,
                _tracker,
                _runningFlows,
                _thresholds,
                _clock)
            {
                CommittedReportSeconds = CommittedReportSeconds,
                UnmatchedKeySample = UnmatchedKeySample,
            };

            EtlLog.PipelineConfigurationLoaded(_logger, configuration.Name, "Graph");
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

        // A domain read is already dispatched to the implementation, so what arrives IS the engine row.
        if (configuration is GraphPipelineConfiguration graph)
        {
            return Create(graph);
        }

        return GenericResult<IEtlPipeline>.Failure(
            EtlLog.PipelineCreationFailed(_logger, "unknown",
                $"Invalid configuration type. Expected GraphPipelineConfiguration, got {configuration.GetType().Name}"));
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
