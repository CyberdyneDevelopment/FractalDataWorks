using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Flow;
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
        IEtlGateway? etlGateway = null,
        IRuleNotifier? notifier = null,
        BuiltCompilation? built = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>? fieldChains = null,
        GraphTestRun? test = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(links);
        _destinations = [];
        PipelineName = pipelineName;
        PipelineId = pipelineId;
        Notifier = notifier;
        Built = built;
        FieldChains = fieldChains is null ? NoChains : fieldChains;
        Test = test;
        ExecutionId = executionId;
        ConnectionProvider = connectionProvider;
        Gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        EtlGateway = etlGateway;
        LoadExecutor = loadExecutor ?? throw new ArgumentNullException(nameof(loadExecutor));
        StartedAt = startedAt;
        Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        ExecutionId = shared.ExecutionId;
        ConnectionProvider = shared.ConnectionProvider;
        Gateway = shared.Gateway;
        EtlGateway = shared.EtlGateway;
        LoadExecutor = shared.LoadExecutor;
        StartedAt = shared.StartedAt;
        Parameters = parameters;
        Logger = shared.Logger;
    }

    /// <summary>
    /// Makes the context a data flow step builds its components with: the same run, with the links of that step, which are what
    /// tell a component its input nodes. The destinations it adds are the run's, so the run counts rows loaded across every step.
    /// </summary>
    /// <param name="links">The links of the data flow step.</param>
    /// <returns>The context for that step.</returns>
    public FlowBuildContext ForLinks(IReadOnlyList<PipelineLinkConfiguration> links)
    {
        ArgumentNullException.ThrowIfNull(links);
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
        ArgumentNullException.ThrowIfNull(values);
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
        builtSets: Built?.Sets);

    /// <summary>Gives how many rows the destinations built so far have loaded, across the whole run.</summary>
    /// <returns>The rows loaded.</returns>
    public long LoadedRows() => _destinations.Sum(destination => (long)destination.RowsLoaded);

    /// <summary>Records a destination the run will ask how many rows it loaded.</summary>
    /// <param name="destination">The destination.</param>
    public void Add(FlowLoadDestination destination) => _destinations.Add(destination);
}
