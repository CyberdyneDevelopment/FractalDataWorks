using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.EtlGateway;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Settings;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// A graph pipeline (FDW-1396): the stored components and links, turned into a flow graph and run by the row-flow engine.
/// </summary>
/// <remarks>
/// <para>
/// A run reads each source through the data gateway, moves rows between components in batches along the links, and loads each
/// destination through the same load mechanism a pipeline's own load uses. A run takes the parameters the graph declares (FDW-1549):
/// the values are checked against the declared names, types and required flags, then declared in the root scope beside the
/// variables. It does not yet run in test mode, fire notification or start-pipeline hooks, or move a watermark: a run that is
/// asked for any of them is refused, not run without them.
/// </para>
/// </remarks>
public sealed class GraphPipeline : EtlPipelineBase
{
    private readonly GraphPipelineConfiguration _configuration;
    private readonly ILogger<GraphPipeline> _logger;
    private readonly IFlowExecutor _flowExecutor;
    private readonly IDataGatewayProvider? _dataGateways;
    private readonly IConnectionProvider? _connectionProvider;
    private readonly IDataStoreProvider? _dataStoreProvider;
    private readonly IDataSetConfigurationProvider? _dataSets;
    private readonly IConnectionTypeResolver? _connectionTypes;
    private readonly Func<IPipelineRunStarter>? _runStarter;
    private readonly IEtlGateway? _etlGateway;
    private readonly IRuleNotifier? _notifier;
    private readonly ITransformationConfigurationProvider? _transformations;
    private readonly IEffectiveSettingsProvider? _settings;
    private readonly IExecutionTracker? _tracker;
    private int _isExecuting;

    /// <summary>Initializes a new instance of the <see cref="GraphPipeline"/> class.</summary>
    /// <param name="configuration">The stored graph.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="flowExecutor">Runs the flow graph.</param>
    /// <param name="dataGateways">The data gateway provider a source reads through.</param>
    /// <param name="connectionProvider">The connection provider a destination's load checks capabilities with.</param>
    /// <param name="dataStoreProvider">The data store provider that resolves a container destination.</param>
    /// <param name="dataSets">The data set configuration provider that resolves a data set destination.</param>
    /// <param name="connectionTypes">Finds a connection's type, so a load is checked against the capabilities the type lists.</param>
    /// <param name="runStarter">Resolves the run starter an Execute Pipeline step starts its pipeline through, only when a step needs one (optional; required to run a graph that has an Execute Pipeline step).</param>
    /// <param name="etlGateway">The ETL gateway a SQL source or Execute SQL step runs its script through (optional; a graph with either refuses to build without it).</param>
    /// <param name="notifier">What sends a notification through a rule (optional; a graph with a Notify step refuses to run without it).</param>
    /// <param name="transformations">Reads the Built transformations a Compute tool applies (optional; a graph with a Compute tool refuses to run without it).</param>
    /// <param name="settings">The effective settings the transformations are compiled under (optional, as above).</param>
    /// <param name="tracker">Records how the steps of a tracked run went as child executions of the run (optional: an untracked host passes none and nothing is recorded).</param>
    public GraphPipeline(
        GraphPipelineConfiguration configuration,
        ILogger<GraphPipeline> logger,
        IFlowExecutor flowExecutor,
        IDataGatewayProvider? dataGateways = null,
        IConnectionProvider? connectionProvider = null,
        IDataStoreProvider? dataStoreProvider = null,
        IDataSetConfigurationProvider? dataSets = null,
        IConnectionTypeResolver? connectionTypes = null,
        Func<IPipelineRunStarter>? runStarter = null,
        IEtlGateway? etlGateway = null,
        IRuleNotifier? notifier = null,
        ITransformationConfigurationProvider? transformations = null,
        IEffectiveSettingsProvider? settings = null,
        IExecutionTracker? tracker = null)
        : base(logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? NullLogger<GraphPipeline>.Instance;
        _flowExecutor = flowExecutor ?? throw new ArgumentNullException(nameof(flowExecutor));
        _dataGateways = dataGateways;
        _connectionProvider = connectionProvider;
        _dataStoreProvider = dataStoreProvider;
        _dataSets = dataSets;
        _connectionTypes = connectionTypes;
        _runStarter = runStarter;
        _etlGateway = etlGateway;
        _notifier = notifier;
        _transformations = transformations;
        _settings = settings;
        _tracker = tracker;
    }

    /// <inheritdoc />
    public override string Id => _configuration.Id.ToString();

    /// <inheritdoc />
    public override string Name => _configuration.Name;

    /// <inheritdoc />
    public override string PipelineType => "Graph";

    /// <inheritdoc />
    public override bool IsExecuting => _isExecuting == 1;

    /// <inheritdoc />
    public override IGenericResult Validate() => GraphShapeValidator.Validate(_configuration, _logger);

    /// <inheritdoc />
    public override Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(CancellationToken cancellationToken = default)
        => Execute(PipelineExecutionOptions.Production, cancellationToken);

    /// <inheritdoc />
    public override async Task<IGenericResult<IEtlPipelineExecutionResult>> Execute(
        PipelineExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
        {
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionFailed(_logger, Name, Guid.Empty, "Pipeline is already executing"));
        }

        // A run the executor tracks is that execution, so the id a notification, a started pipeline or a loaded row carries is the
        // one the execution list shows. A run nobody tracks has no execution to be, so it is its own new one (as the other engines').
        var executionId = options.ExecutionId is { } tracked ? tracked : Guid.NewGuid();
        var result = new EtlPipelineExecutionResult(executionId);
        try
        {
            EtlLog.ExecutionStarted(_logger, Name, executionId);
            var ran = await Run(options, result, cancellationToken).ConfigureAwait(false);
            result.Complete();
            return ran.IsSuccess
                ? GenericResult<IEtlPipelineExecutionResult>.Success(result)
                : ran.ToNewResult<IEtlPipelineExecutionResult>();
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            result.AddError($"Pipeline execution was cancelled: {ex.Message}");
            result.Complete();
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionFailed(_logger, Name, executionId, "Execution cancelled"));
        }
        catch (Exception ex)
        {
            result.AddError(ex.Message);
            result.Complete();
            return GenericResult<IEtlPipelineExecutionResult>.Failure(
                EtlLog.ExecutionFailed(_logger, Name, executionId, ex.Message));
        }
        finally
        {
            Interlocked.Exchange(ref _isExecuting, 0);
        }
    }

    private async Task<IGenericResult> Run(PipelineExecutionOptions options, EtlPipelineExecutionResult result, CancellationToken cancellationToken)
    {
        var supplied = PipelineRunParameterValues.Verify(_configuration.Parameters, options.Parameters, _logger);
        if (supplied.IsFailure)
        {
            return supplied;
        }

        var valid = Validate();
        if (valid.IsFailure)
        {
            return valid;
        }

        if (_dataGateways is null)
        {
            return GenericResult.Failure(GraphLog.RunDependencyMissing(_logger, Name, "the data gateway"));
        }

        var notifiable = await CheckNotifications(cancellationToken).ConfigureAwait(false);
        if (notifiable.IsFailure)
        {
            return notifiable;
        }

        return await RunSteps(options, result, _dataGateways, RunOptions(options), cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult> RunSteps(
        PipelineExecutionOptions options, EtlPipelineExecutionResult result, IDataGatewayProvider dataGateways, FlowRunOptions flowOptions, CancellationToken cancellationToken)
    {
        var variables = GraphVariables.Declare(_configuration, options.Parameters, options.RunStarted, _logger);
        if (variables.IsFailure || variables.Value is null)
        {
            return variables;
        }

        var resolved = await ResolveStages(cancellationToken).ConfigureAwait(false);
        if (resolved.IsFailure || resolved.Value is null)
        {
            return resolved;
        }

        var executor = new PipelineLoadExecutor(
            new LoadTargetResolver(_dataSets, _dataStoreProvider), _connectionProvider, _connectionTypes, _logger);
        var scope = GraphScope.RenderValues(_configuration, ParameterValues(options), _logger);
        if (scope.IsFailure || scope.Value is null)
        {
            return scope;
        }

        var context = new FlowBuildContext(
            Name,
            _configuration.Id,
            result.ExecutionId,
            dataGateways.ByName("Main"),
            executor,
            _connectionProvider,
            DateTimeOffset.UtcNow,
            scope.Value,
            [],
            _logger,
            _etlGateway,
            _notifier,
            resolved.Value.Built,
            resolved.Value.Chains,
            options.IsTestMode ? new GraphTestRun(options.MaxRowsPerSource, options.SkipDestinationWrites) : null);

        var pipeline = GraphFlowBuilder.Build(_configuration, context);
        if (pipeline.IsFailure || pipeline.Value is null)
        {
            return pipeline;
        }

        var identity = IdentityOf(options, result.ExecutionId);
        var tally = new FlowRunTally(_flowExecutor);
        var stepContext = new FlowStepContext(
            variables.Value,
            tally,
            flowOptions,
            _runStarter?.Invoke(),
            identity,
            ReporterOf(options),
            null,
            options.PauseGate);
        var ran = await pipeline.Value.Run(stepContext, cancellationToken).ConfigureAwait(false);
        if (ran.IsFailure)
        {
            return ran;
        }

        RecordCounts(result, tally, context);
        return GenericResult.Success();
    }

    // Rows read are the rows that left a source; rows that reached a destination are the rows transformed on the way; rows
    // loaded are what the destinations' loads affected. Each is summed across every flow the run ran.
    private void RecordCounts(EtlPipelineExecutionResult result, FlowRunTally tally, FlowBuildContext context)
    {
        result.RecordsExtracted = Saturate(tally.Extracted);
        result.RecordsTransformed = Saturate(tally.Transformed);
        result.RecordsLoaded = Saturate(context.Destinations.Sum(destination => (long)destination.RowsLoaded));
        GraphLog.RunFinished(
            _logger, Name, _configuration.Components.Count, result.RecordsExtracted, result.RecordsLoaded);
    }

    // The run's counts of rejected rows need no setting: they hold no row (FDW-1720), so there is no limit to state.
    private FlowRunOptions RunOptions(PipelineExecutionOptions options) => new() { LinkCapacity = _configuration.LinkCapacity, PauseGate = options.PauseGate };

    // Why before anything moves: a Built transformation or a chain of named transformations that no longer resolves must fail the run
    // naming it, not be found halfway through the data.
    private async Task<IGenericResult<ResolvedStages>> ResolveStages(CancellationToken cancellationToken)
    {
        var components = _configuration.Components.ToList();
        var stages = components.SelectMany(component => component.Stages).ToList();
        var built = await BuiltCalculationResolver.Resolve(stages, _transformations, _settings, _logger, cancellationToken).ConfigureAwait(false);
        if (built.IsFailure || built.Value is null)
        {
            return built.ToNewResult<ResolvedStages>();
        }

        var chains = await FieldTransformChains.Resolve(components, _transformations, _logger, cancellationToken).ConfigureAwait(false);
        return chains.IsFailure || chains.Value is null
            ? chains.ToNewResult<ResolvedStages>()
            : GenericResult<ResolvedStages>.Success(new ResolvedStages(built.Value, chains.Value));
    }

    // Why before anything moves: a Notify component that cannot send (no notification service, or a rule that does not exist) is found
    // here, not after the data has been loaded.
    private async Task<IGenericResult> CheckNotifications(CancellationToken cancellationToken)
    {
        foreach (var notify in _configuration.Components.Where(component => string.Equals(component.ComponentType, "Notify", StringComparison.OrdinalIgnoreCase)))
        {
            if (_notifier is null)
            {
                return GenericResult.Failure(NotifyLog.NotifyRuleUnavailable(_logger, Name, notify.Name));
            }

            if (notify.NotificationRuleName is { } rule)
            {
                var exists = await _notifier.Exists(rule, cancellationToken).ConfigureAwait(false);
                if (exists.IsFailure)
                {
                    return exists;
                }
            }
        }

        return GenericResult.Success();
    }

    // The values the run was started with, which the components are built with (FDW-1746): a source that names {{Name}} and a setting
    // bound to a parameter read them here. They are the ones the run has just verified against the declared parameters; a run started
    // with none has none, and nothing stands in for a missing one.
    internal static Dictionary<string, object?> ParameterValues(PipelineExecutionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Parameters is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(options.Parameters, StringComparer.OrdinalIgnoreCase);
    }

    // A run that is tracked records how its steps went under its execution; an untracked run records nothing.
    private ExecutionFlowReporter? ReporterOf(PipelineExecutionOptions options) =>
        _tracker is { } tracker && options.ExecutionId is { } runId
            ? new ExecutionFlowReporter(tracker, runId, options.TenantId, _logger)
            : null;

    // A run is tracked when it states who it is for; only then may a step start another pipeline as a child of this run. An
    // untracked run has no identity, and a step that needs one refuses.
    private static FlowRunIdentity? IdentityOf(PipelineExecutionOptions options, Guid executionId) =>
        options.ExecutionId is not null && options.TenantId is { } tenantId && options.RunRoles is { } roles
            ? new FlowRunIdentity(executionId, tenantId, roles)
            : null;

    private static int Saturate(long value) => value > int.MaxValue ? int.MaxValue : (int)value;
}
