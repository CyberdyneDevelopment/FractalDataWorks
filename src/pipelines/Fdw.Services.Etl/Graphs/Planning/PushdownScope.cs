using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// What the planner reads one graph through (FDW-6969): its components and row lines, the gateway that describes its containers, each container
/// described once, and the logger its trace goes to.
/// </summary>
public sealed class PushdownScope
{
    private readonly IPushdownPlanningGateway _gateway;
    private readonly HashSet<string> _completing;
    private readonly string? _readEnd;
    private readonly Dictionary<DataStoreTarget, IGenericResult<PushdownContainer>> _described = [];

    /// <summary>Initializes a new instance of the <see cref="PushdownScope"/> class.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="gateway">The gateway that describes its containers.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="readEnd">The component a read takes its rows from, which is a consumer no stored line shows; null when the graph is not read.</param>
    public PushdownScope(IGraphPipelineConfiguration graph, IPushdownPlanningGateway gateway, ILogger logger, string? readEnd)
    {
        Graph = graph;
        _readEnd = readEnd;
        _gateway = gateway;
        Logger = logger;
        Rows = CompletionLines.Rows(graph.Links, graph.Components);
        _completing = new HashSet<string>(
            CompletionLines.Completions(graph.Links, graph.Components).SelectMany(line => new[] { line.FromComponent, line.ToComponent }), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets the graph.</summary>
    public IGraphPipelineConfiguration Graph { get; }

    /// <summary>Gets the logger.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets the graph's row lines.</summary>
    public IReadOnlyList<PipelineLinkConfiguration> Rows { get; }

    /// <summary>Gets whether a completion line leaves or reaches a component.</summary>
    /// <param name="component">The component's name.</param>
    /// <returns>True when one does.</returns>
    public bool Completes(string component) => _completing.Contains(component);

    /// <summary>Finds a component by name.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The component, or null when the graph has none by that name.</returns>
    public PipelineComponentConfiguration? Component(string name) =>
        Graph.Components.FirstOrDefault(component => string.Equals(component.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Lists the row lines into one input of a component.</summary>
    /// <param name="component">The component's name.</param>
    /// <param name="input">The input's name.</param>
    /// <returns>The lines.</returns>
    public IReadOnlyList<PipelineLinkConfiguration> Into(string component, string input) =>
        [.. Rows.Where(line => string.Equals(line.ToComponent, component, StringComparison.OrdinalIgnoreCase)
            && string.Equals(line.ToInput, input, StringComparison.OrdinalIgnoreCase))];

    /// <summary>Counts the places a component's rows go: its row lines out, its outputs stated as dropped, and the read when it ends there.</summary>
    /// <param name="component">The component's name.</param>
    /// <returns>The count.</returns>
    public int Consumers(string component) =>
        Rows.Count(line => string.Equals(line.FromComponent, component, StringComparison.OrdinalIgnoreCase))
        + (string.Equals(_readEnd, component, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
        + Graph.DroppedOutputs.Count(output => string.Equals(output.ComponentName, component, StringComparison.OrdinalIgnoreCase));

    /// <summary>Describes a container through the gateway, once per container.</summary>
    /// <param name="target">The data store, path and container.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The description, or why the gateway gives none.</returns>
    public async Task<IGenericResult<PushdownContainer>> Describe(DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        if (_described.TryGetValue(target, out var known))
        {
            return known;
        }

        var described = await _gateway.Describe(target, cancellationToken).ConfigureAwait(false);
        _described[target] = described;
        return described;
    }
}
