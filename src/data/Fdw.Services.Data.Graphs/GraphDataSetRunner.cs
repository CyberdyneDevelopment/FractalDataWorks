using System.Collections.Generic;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Pipelines.Loads;
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
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="GraphDataSetRunner"/> class.</summary>
    /// <param name="options">The row, time and link limits of a read.</param>
    /// <param name="logger">The logger.</param>
    public GraphDataSetRunner(GraphDataSetReadOptions options, ILogger<GraphDataSetRunner>? logger = null)
    {
        _options = options;
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
        RunGraph(gateway, source.Name, GraphDataSetPipelines.Read(source, _options.LinkCapacity), new GraphEnd(component, output), null, cancellationToken);

    // The run of a graph that is read and not loaded, under the limits of a read: a stored data set's, or a draft's with a test run that caps the rows each source gives (FDW-5454).
    internal async Task<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>> RunGraph(
        IDataGateway gateway, string dataSet, GraphPipelineConfiguration graph, GraphEnd end, GraphTestRun? test, CancellationToken cancellationToken)
    {
        var collector = new GraphReadCollector(dataSet, _options.MaxRows, _logger);
        var variables = GraphVariables.Declare(graph, null, _logger);
        if (variables.IsFailure || variables.Value is null)
        {
            return RunFailed(dataSet, variables);
        }

        var built = GraphFlowBuilder.BuildRead(graph, BuildContext(graph, gateway, test), new GraphReadTerminal(end.Component, end.Output, collector.Write));
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

    // Why no run record, tracking or live view: a read has nothing to record, so the context states only what the sources need, the gateway the read arrived on.
    private FlowBuildContext BuildContext(GraphPipelineConfiguration graph, IDataGateway gateway, GraphTestRun? test) => new(
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
