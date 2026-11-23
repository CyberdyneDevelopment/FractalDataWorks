using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.EtlGateway;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// What a component builder needs from the run it builds for (FDW-1396): the gateway a source reads through, the executor a
/// destination loads through, and which input nodes the graph's links feed.
/// </summary>
public sealed class FlowBuildContext
{
    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransformation>> NoChains =
        new Dictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>();

    private readonly ILookup<string, string> _inputs;
    private readonly List<FlowLoadDestination> _destinations;

    /// <summary>Initializes a new instance of the <see cref="FlowBuildContext"/> class.</summary>
    /// <param name="pipelineName">The pipeline, for messages and the executor's.</param>
    /// <param name="pipelineId">The pipeline's durable id, which a notification carries.</param>
    /// <param name="executionId">The run's execution id, which a stage's transform context carries.</param>
    /// <param name="gateway">The data gateway a source reads through.</param>
    /// <param name="loadExecutor">The executor a destination loads through.</param>
    /// <param name="connectionProvider">The connection provider a stage's transform context offers (optional).</param>
    /// <param name="startedAt">The moment the run began.</param>
    /// <param name="parameters">The run's typed parameter values.</param>
    /// <param name="links">The graph's links, from which each component's input nodes are read.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="etlGateway">The ETL gateway a SQL source or Execute SQL step runs its script through (optional; a graph with either refuses to build without it).</param>
    /// <param name="notifier">What sends a notification through a rule (optional; a graph with a Notify step refuses to build without it).</param>
    /// <param name="built">The Built transformations the graph's Compute tools apply, compiled for the run (optional; absent when the graph has no Compute tool).</param>
    /// <param name="fieldChains">The chains of named transformations the field mappings of the graph's tools run, resolved for the run (optional; absent when no mapping states one).</param>
    /// <param name="test">How a test run differs from a real run (optional; absent for a real run).</param>
    /// <param name="clock">The run's clock, which a stage's transform context offers a calculation that reads the time (optional; absent when the host has none).</param>
    /// <param name="commitReportInterval">How often a destination writes the rows it has committed to the run's record (FDW-4957); zero writes after every commit.</param>
    /// <param name="unmatchedKeySample">How many unmatched keys an update or delete destination's report carries (FDW-4988); zero carries only the count.</param>
    /// <param name="contributions">
    /// The build contributions the host registered, which a builder's needs are judged against (FDW-6132). Required (FDW-6240), so no place that builds a graph
    /// leaves them out by omission: a graph pipeline hands over the ones the graph type registered, and a place that has none says so with
    /// <see cref="FlowBuildContributions.None"/>, as the Graph data set runner does.
    /// </param>
    public FlowBuildContext(
        string pipelineName,
        Guid pipelineId,
        Guid executionId,
        IDataGateway gateway,
        PipelineLoadExecutor loadExecutor,
        IConnectionProvider? connectionProvider,
        DateTimeOffset startedAt,
        IReadOnlyDictionary<string, object?> parameters,
        IReadOnlyList<PipelineLinkConfiguration> links,
        ILogger logger,
        TimeSpan commitReportInterval,
        int unmatchedKeySample,
        FlowBuildContributions contributions,
        IEtlGateway? etlGateway = null,
        IRuleNotifier? notifier = null,
        BuiltCompilation? built = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>? fieldChains = null,
        GraphTestRun? test = null,
        TimeProvider? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        _destinations = [];
        PipelineName = pipelineName;
        PipelineId = pipelineId;
        Notifier = notifier;
        Built = built;
        FieldChains = fieldChains is null ? NoChains : fieldChains;
        Test = test;
        Clock = clock;
        Contributions = contributions;
        ExecutionId = executionId;
        ConnectionProvider = connectionProvider;
        Gateway = gateway;
        EtlGateway = etlGateway;
        LoadExecutor = loadExecutor;
        StartedAt = startedAt;
        Parameters = parameters;
        Logger = logger;
        ArgumentOutOfRangeException.ThrowIfLessThan(commitReportInterval, TimeSpan.Zero);
        CommitReportInterval = commitReportInterval;
        ArgumentOutOfRangeException.ThrowIfNegative(unmatchedKeySample);
        UnmatchedKeySample = unmatchedKeySample;
        _inputs = links.ToLookup(link => link.ToComponent, link => link.ToInput, StringComparer.OrdinalIgnoreCase);
    }

    private FlowBuildContext(FlowBuildContext shared, IReadOnlyList<PipelineLinkConfiguration> links)
        : this(shared, links.ToLookup(link => link.ToComponent, link => link.ToInput, StringComparer.OrdinalIgnoreCase), shared.Parameters)
    {
    }

    private FlowBuildContext(FlowBuildContext shared, ILookup<string, string> inputs, IReadOnlyDictionary<string, object?> parameters)
    {
        _destinations = shared._destinations;
        _inputs = inputs;
        PipelineName = shared.PipelineName;
        PipelineId = shared.PipelineId;
        Notifier = shared.Notifier;
        Built = shared.Built;
        FieldChains = shared.FieldChains;
        Test = shared.Test;
        Clock = shared.Clock;
        Contributions = shared.Contributions;
        ExecutionId = shared.ExecutionId;
        ConnectionProvider = shared.ConnectionProvider;
        Gateway = shared.Gateway;
        EtlGateway = shared.EtlGateway;
        LoadExecutor = shared.LoadExecutor;
        StartedAt = shared.StartedAt;
        Parameters = parameters;
        Logger = shared.Logger;
        CommitReportInterval = shared.CommitReportInterval;
        UnmatchedKeySample = shared.UnmatchedKeySample;
    }

    /// <summary>
    /// Makes the context a data flow step builds its components with: the same run, with the links of that step, which are what
    /// tell a component its input nodes. The destinations it adds are the run's, so the run counts rows loaded across every step.
    /// </summary>
    /// <param name="links">The links of the data flow step.</param>
    /// <returns>The context for that step.</returns>
    public FlowBuildContext ForLinks(IReadOnlyList<PipelineLinkConfiguration> links)
    {
        return new FlowBuildContext(this, links);
    }

    /// <summary>
    /// Makes the context a component built when its stage starts uses (FDW-1984): the same run, with the variables as the stages before left them
    /// added to its parameters, so a tool's <c>{{Name}}</c> reference can name a value an earlier stage calculated.
    /// </summary>
    /// <param name="values">The variables in scope when the stage starts.</param>
    /// <returns>The context for that component. A name that is a run parameter keeps the run's value.</returns>
    public FlowBuildContext WithValues(IReadOnlyDictionary<string, object?> values)
    {
        var merged = new Dictionary<string, object?>(Parameters, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in values)
        {
            _ = merged.TryAdd(name, value);
        }

        return new FlowBuildContext(this, _inputs, merged);
    }

    /// <summary>Gets the pipeline's name.</summary>
    public string PipelineName { get; }

    /// <summary>Gets the pipeline's durable id.</summary>
    public Guid PipelineId { get; }

    /// <summary>Gets what sends a notification through a rule, or <see langword="null"/> when the host has none.</summary>
    public IRuleNotifier? Notifier { get; }

    /// <summary>Gets the Built transformations the run's Compute tools apply, compiled before any row moves, or <see langword="null"/> when the graph has none.</summary>
    public BuiltCompilation? Built { get; }

    /// <summary>Gets how a test run differs from a real run; none for a real run.</summary>
    public GraphTestRun? Test { get; }

    /// <summary>Gets the run's clock; <see langword="null"/> when the host registered none.</summary>
    public TimeProvider? Clock { get; }

    /// <summary>Gets the build contributions the host registered (FDW-6132).</summary>
    public FlowBuildContributions Contributions { get; }

    /// <summary>Gets how often a destination writes the rows it has committed to the run's record (FDW-4957); zero writes after every commit.</summary>
    public TimeSpan CommitReportInterval { get; }

    /// <summary>Gets how many unmatched keys an update or delete destination's report carries (FDW-4988); zero carries only the count.</summary>
    public int UnmatchedKeySample { get; }

    /// <summary>Gets the chains of named transformations the mappings on the graph's component inputs run, by mapping id; none when no mapping states one.</summary>
    public IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransformation>> FieldChains { get; }

    /// <summary>Gets the run's execution id.</summary>
    public Guid ExecutionId { get; }

    /// <summary>Gets the connection provider a stage's transform context offers; <see langword="null"/> when the run has none.</summary>
    public IConnectionProvider? ConnectionProvider { get; }

    /// <summary>Gets the data gateway a source reads through.</summary>
    public IDataGateway Gateway { get; }

    /// <summary>Gets the ETL gateway a script source or step runs its script through; null when the host has none (FDW-1479).</summary>
    public IEtlGateway? EtlGateway { get; }

    /// <summary>Gets the executor a destination loads through.</summary>
    public PipelineLoadExecutor LoadExecutor { get; }

    /// <summary>Gets the moment the run began.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>Gets the run's typed parameter values.</summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>Gets the logger a refusal is written to.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets what claims a step that acts outside the run before it acts (FDW-6139); null for a host that registers none, which then refuses to build such a step.</summary>
    public Fdw.Services.Etl.Abstractions.Execution.IPipelineStepClaims? StepClaims { get; init; }

    /// <summary>Gets what an Ask component keeps its request with and judges its call by (FDW-6142); null for a host that registers none, which then refuses to build such a step.</summary>
    public FlowAskServices? AskServices { get; init; }

    /// <summary>Gets the destinations built so far, so the run can add up what they loaded.</summary>
    public IReadOnlyList<FlowLoadDestination> Destinations => _destinations;

    /// <summary>Lists the input nodes of a component that the graph's links feed, in link order, each once.</summary>
    /// <param name="component">The component's name.</param>
    /// <returns>The input node names.</returns>
    public IReadOnlyList<string> InputsOf(string component) =>
        [.. _inputs[component].Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Makes the transform context a stage runs with. It offers the run's gateway and connections and its parameters; it offers no
    /// compiled calculations, which is why the stage kinds that need them are not graph components.
    /// </summary>
    /// <returns>A new context.</returns>
    public TransformContext NewTransformContext() => new(
        ExecutionId,
        Logger,
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
        connectionProvider: ConnectionProvider,
        dataGateway: Gateway,
        parameters: Parameters,
        builtCalculations: Built?.Calculations,
        builtSets: Built?.Sets,
        clock: Clock);

    /// <summary>Gives how many rows the destinations built so far have loaded, across the whole run.</summary>
    /// <returns>The rows loaded.</returns>
    public long LoadedRows() => _destinations.Sum(destination => (long)destination.RowsLoaded);

    /// <summary>Judges what a component's kind needs against the contributions the host registered (FDW-6132).</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="needs">The contribution types its kind's builder needs.</param>
    /// <returns>Success when every one is registered; otherwise a failure with one message for each that is not.</returns>
    public IGenericResult NeedsMet(PipelineComponentConfiguration component, IReadOnlyList<Type> needs)
    {
        var unmet = Contributions.Unmet(needs);
        return unmet.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(unmet.Select(need => GraphLog.ComponentServiceMissing(Logger, component.Name, component.ComponentType, need.ToString())));
    }

    /// <summary>Gives a builder the contribution of a type the host registered (FDW-6132).</summary>
    /// <typeparam name="T">The contribution type, one the builder names in its needs.</typeparam>
    /// <param name="component">The stored component being built, named when the contribution is missing.</param>
    /// <returns>The contribution, or the failure that names the component, its kind and the contribution.</returns>
    public IGenericResult<T> Contribution<T>(PipelineComponentConfiguration component)
        where T : class, IFlowBuildContribution =>
        Contributions.Find<T>() is { } found
            ? GenericResult<T>.Success(found)
            : GenericResult<T>.Failure(GraphLog.ComponentServiceMissing(Logger, component.Name, component.ComponentType, typeof(T).ToString()));

    /// <summary>Gives a builder the value of a setting its component type declares that no column of the component holds (FDW-6133).</summary>
    /// <param name="component">The stored component being built.</param>
    /// <param name="name">The setting's name, as the component type declares it.</param>
    /// <returns>The value, read when the step runs from <see cref="ComponentSettingValue.Read"/>, or the refusal that says why there is none.</returns>
    public IGenericResult<ComponentSettingValue> Setting(PipelineComponentConfiguration component, string name) =>
        ComponentSettings.Declared(component, ComponentTypes.ByName(component.ComponentType), name, Logger);

    /// <summary>Records a destination the run will ask how many rows it loaded.</summary>
    /// <param name="destination">The destination.</param>
    public void Add(FlowLoadDestination destination) => _destinations.Add(destination);
}
