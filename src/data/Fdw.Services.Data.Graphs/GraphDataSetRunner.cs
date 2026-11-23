using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Graphs.Planning;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Graphs;

/// <summary>
/// Runs the graph of a Graph data set on the engine, in the host that serves the read, and collects the rows of its fields out (FDW-5291).
/// </summary>
/// <remarks>
/// <para>
/// No run record, tracking or live view: the run is a read, so only the graph and the engine's options are used. The graph's sources read through
/// the gateway the read arrived on, so each source, a nested data set included, is read for the caller with the caller's row rules.
/// </para>
/// <para>
/// A read ends in one of two ways: every row of the fields out, or a named failure. The row limit and the time limit stop the run at the first
/// breach and the rows read so far are dropped, so a part is never presented as the whole.
/// </para>
/// </remarks>
public sealed class GraphDataSetRunner : IGraphDataSetRunner
{
    private readonly GraphDataSetReadOptions _options;
    private readonly IServiceScopeFactory _scopes;
    private readonly FederationSettings? _federation;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GraphDataSetRunner"/> class.</summary>
    /// <param name="options">The row, time and link limits of a read.</param>
    /// <param name="scopes">Opens the scope each read takes its build contributions from (FDW-6240): contributions are scoped, and this runner is held by singletons.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="federation">
    /// The FDW-level federation settings (FDW-6806): the strategy a graph's reads across data stores run under (FDW-6970). Without them a graph that joins
    /// reads of more than one store is refused; every other graph reads as before.
    /// </param>
    public GraphDataSetRunner(
        GraphDataSetReadOptions options, IServiceScopeFactory scopes, ILogger<GraphDataSetRunner>? logger = null, FederationSettings? federation = null)
    {
        _options = options;
        _scopes = scopes;
        _federation = federation;
        _logger = logger ?? NullLogger<GraphDataSetRunner>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Read(
        IDataGateway gateway, IGraphDataSetConfiguration dataSet, CancellationToken cancellationToken = default)
    {
        var limits = CheckLimits(dataSet.Name);
        if (limits.IsFailure)
        {
            return limits.ToNewResult<IReadOnlyList<IDictionary<string, object?>>>();
        }

        // Why both stated or neither (the store's own check): a graph with components and no stated end for its fields out has nowhere to read its rows from, so it is not bound.
        if (dataSet.FieldsOutComponent is not { Length: > 0 } component || dataSet.FieldsOutOutput is not { Length: > 0 } output || dataSet.Components.Count == 0)
        {
            return NotBound(dataSet.Name);
        }

        return await Run(gateway, dataSet, component, output, cancellationToken).ConfigureAwait(false);
    }

    private Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> Run(
        IDataGateway gateway, IGraphDataSetConfiguration source, string component, string output, CancellationToken cancellationToken) =>
        RunGraph(gateway, source.Name, GraphDataSetPipelines.Read(source, _options.LinkCapacity), new GraphEnd(component, output), null, source.FederationStrategy, [.. source.FederationOptions], cancellationToken);

    // The run of a graph that is read and not loaded, under the limits of a read: a stored data set's, or a draft's with a test run that caps the rows each source gives (FDW-5454).
    internal async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> RunGraph(
        IDataGateway gateway, string dataSet, GraphPipelineConfiguration graph, GraphEnd end, GraphTestRun? test, string? federationStrategy,
        IReadOnlyList<DataSetFederationOptionConfiguration> federationOptions, CancellationToken cancellationToken)
    {
        var collector = new GraphReadCollector(dataSet, _options.MaxRows, _logger);
        var variables = GraphVariables.Declare(graph, null, _logger);
        if (variables.IsFailure || variables.Value is null)
        {
            return RunFailed(dataSet, variables);
        }

        // Why a test run is not planned (FDW-6969): it caps the rows each source gives, and a pushed query reads its sources as one.
        var plan = test is null
            ? await GraphPushdownPlanner.Plan(graph, gateway, end, _logger, cancellationToken).ConfigureAwait(false)
            : InMemoryForTest(graph.Name);
        // Why a scope of the read's own (FDW-6240): build contributions are scoped, as a run's services are, and this runner is held by the singleton data gateway factories, so each read
        // takes its contributions from a scope that ends with it, resolved as the host's start resolves them; two reads never share an instance.
        using var scope = _scopes.CreateScope();
        var contributions = FlowBuildNeeds.Resolve(scope.ServiceProvider, _logger);
        if (contributions.IsFailure || contributions.Value is null)
        {
            return RunFailed(dataSet, contributions);
        }

        var built = Build(graph, BuildContext(graph, gateway, test, contributions.Value), new GraphReadTerminal(end.Component, end.Output, collector.Write), plan, dataSet, federationStrategy, federationOptions);
        if (built.IsFailure || built.Value is null)
        {
            return RunFailed(dataSet, built);
        }

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stop.CancelAfter(TimeSpan.FromSeconds(_options.MaxRunSeconds));
        var step = new FlowStepContext(
            variables.Value, new FlowExecutor(), new FlowRunOptions { LinkCapacity = _options.LinkCapacity }, null, null);
        var ran = await built.Value.Run(step, stop.Token).ConfigureAwait(false);
        return Outcome(dataSet, ran, collector, stop.IsCancellationRequested && !cancellationToken.IsCancellationRequested);
    }

    // A graph with no reads across data stores builds as before; one with them gets the gate its federation strategy lets them run through (FDW-6970).
    private IGenericResult<FlowPipeline> Build(
        GraphPipelineConfiguration graph, FlowBuildContext context, GraphReadTerminal terminal, GraphPushdownPlan plan, string dataSet, string? federationStrategy,
        IReadOnlyList<DataSetFederationOptionConfiguration> federationOptions)
    {
        if (plan.FederatedReads.Count == 0)
        {
            return GraphFlowBuilder.BuildRead(graph, context, terminal, plan);
        }

        var gate = Gate(graph.Name, plan, dataSet, federationStrategy, federationOptions);
        return gate.IsFailure || gate.Value is null ? gate.ToNewResult<FlowPipeline>() : GraphFlowBuilder.BuildRead(graph, context, terminal, plan, gate.Value);
    }

    // The gate of the strategy the graph's federated reads run under, resolved where a Federated data set's is (FDW-6901): the data set's own strategy,
    // else the FDW-level default, and each option the data set's own value, else the FDW-level value, else the strategy's declared one. Reads in
    // parallel lets MaxParallelReads of them run at once, else one; OrdersSources starts the build side first, the order that lets a join's probe side
    // stream past what it holds. A strategy or option that cannot be resolved refuses the read.
    private IGenericResult<FlowReadGate> Gate(
        string graph, GraphPushdownPlan plan, string dataSet, string? federationStrategy, IReadOnlyList<DataSetFederationOptionConfiguration> federationOptions)
    {
        var resolved = FederationResolution.Resolve(_federation, dataSet, federationStrategy, federationOptions, _logger);
        if (resolved.IsFailure || resolved.Value is not { } federation)
        {
            return resolved.ToNewResult<FlowReadGate>();
        }

        var settings = new FlowReadGateSettings(federation.ReadsInParallel ? federation.MaxParallelReads : 1, federation.OrdersSources);
        PushdownLog.FederatedReadsGated(
            _logger,
            graph,
            plan.FederatedReads.Count,
            federation.Strategy,
            $"strategy from {federation.StrategyFrom}; ReadsInParallel {federation.ReadsInParallel} from {federation.ReadsInParallelFrom}; "
            + $"MaxParallelReads {federation.MaxParallelReads} from {federation.MaxParallelReadsFrom}; OrdersSources {federation.OrdersSources} from {federation.OrdersSourcesFrom}; "
            + $"{settings.MaxConcurrentReads} at once, build side first {settings.BuildSideFirst}");
        return GenericResult<FlowReadGate>.Success(new FlowReadGate(settings, plan.FederatedReads.Count(read => read.BuildSide)));
    }

    private GraphPushdownPlan InMemoryForTest(string graph)
    {
        PushdownLog.TestRunInMemory(_logger, graph);
        return GraphPushdownPlan.None;
    }

    // Why no run record, tracking or live view: a read has nothing to record, so the context states only what the sources need, the gateway the read arrived on.
    private FlowBuildContext BuildContext(GraphPipelineConfiguration graph, IDataGateway gateway, GraphTestRun? test, FlowBuildContributions contributions) => new(
        graph.Name,
        graph.Id,
        Guid.NewGuid(),
        gateway,
        new PipelineLoadExecutor(new LoadTargetResolver(null, null), null, null, _logger),
        null,
        DateTimeOffset.UtcNow,
        new Dictionary<string, object?>(StringComparer.Ordinal),
        [],
        _logger,
        TimeSpan.Zero,
        0,
        contributions,
        test: test);

    // Why the time limit is judged by who cancelled and not by the failure text: the engine answers a cancelled run with its own failure, and only the token says whether the limit cancelled it.
    private IGenericResult<IReadOnlyList<IDictionary<string, object?>>> Outcome(
        string dataSet, IGenericResult ran, GraphReadCollector collector, bool timedOut)
    {
        if (collector.Failure is { } overLimit)
        {
            return overLimit.ToNewResult<IReadOnlyList<IDictionary<string, object?>>>();
        }

        if (ran.IsSuccess)
        {
            return GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(collector.Rows);
        }

        return timedOut
            ? GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetTimeLimitExceeded"), _logger, Details(dataSet, "limit", $"{_options.MaxRunSeconds} seconds"))
            : RunFailed(dataSet, ran);
    }

    internal IGenericResult<IReadOnlyList<IDictionary<string, object?>>> NotBound(string dataSet) =>
        GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
            DataServiceResultCodes.ByName("GraphDataSetNotBound"), _logger, Details(dataSet));

    private IGenericResult<IReadOnlyList<IDictionary<string, object?>>> RunFailed(string dataSet, IGenericResult cause) =>
        GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(
            DataServiceResultCodes.ByName("GraphDataSetRunFailed"), _logger, Details(dataSet, "reason", cause.CurrentMessage ?? string.Empty));

    internal IGenericResult CheckLimits(string dataSet)
    {
        if (_options.MaxRows < 1)
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetLimitNotStated"), _logger,
                ResultDetails.Create("dataSet", dataSet, "limit", nameof(GraphDataSetReadOptions.MaxRows), "value", _options.MaxRows));
        }

        if (_options.MaxRunSeconds < 1)
        {
            return GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetLimitNotStated"), _logger,
                ResultDetails.Create("dataSet", dataSet, "limit", nameof(GraphDataSetReadOptions.MaxRunSeconds), "value", _options.MaxRunSeconds));
        }

        return _options.LinkCapacity < 1
            ? GenericResult.Failure(
                DataServiceResultCodes.ByName("GraphDataSetLimitNotStated"), _logger,
                ResultDetails.Create("dataSet", dataSet, "limit", nameof(GraphDataSetReadOptions.LinkCapacity), "value", _options.LinkCapacity))
            : GenericResult.Success();
    }

    private static ResultDetails Details(string dataSet) => ResultDetails.Create("dataSet", dataSet);

    private static ResultDetails Details(string dataSet, string name, object value) => ResultDetails.Create("dataSet", dataSet, name, value);
}
