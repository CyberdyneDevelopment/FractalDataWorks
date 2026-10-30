using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The wiring of one run: a channel per input node, a state per link, and the nodes each component sees (FDW-1386).
/// </summary>
internal sealed class FlowRun
{
    private readonly Dictionary<string, FlowPorts> _ports = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FlowLinkState> _links = [];
    private Stopped? _stopped;
    private readonly FlowRejectedRows _rejected = new();

    private FlowRun()
    {
    }

    /// <summary>Gets the first failure any component reported; null while none has.</summary>
    public IGenericResult? Failure => Volatile.Read(ref _stopped)?.Failure;

    /// <summary>Gets the component whose failure was the first reported; null while none has failed.</summary>
    public string? FailedComponent => Volatile.Read(ref _stopped)?.Component;

    /// <summary>Gets how many rows the error outputs carried and the reasons they gave, never the rows; empty when no row was rejected.</summary>
    public IReadOnlyList<FlowRejection> Rejections => _rejected.Snapshot();

    /// <summary>Gets the state of every link, including dropped outputs.</summary>
    public IReadOnlyList<FlowLinkState> Links => _links;

    /// <summary>Wires a validated graph.</summary>
    /// <param name="graph">The graph; it has passed <see cref="FlowGraphValidator"/>.</param>
    /// <param name="capacity">The batches each link buffers.</param>
    /// <param name="pauseGate">The gate every batch passes before it leaves a component; null when the run cannot be paused.</param>
    /// <returns>The wired run.</returns>
    public static FlowRun Build(FlowGraph graph, int capacity, IFlowPauseGate? pauseGate)
    {
        var run = new FlowRun();
        var inputs = new Dictionary<(string, string), FlowInputState>();
        foreach (var component in graph.Components)
        {
            var ports = new FlowPorts();
            run._ports[component.Name] = ports;
            foreach (var input in component.Inputs)
            {
                var incoming = graph.Links.Count(l => Same(l.ToComponent, component.Name) && Same(l.ToInput, input));
                var crossesStages = graph.Links.Any(l => Same(l.ToComponent, component.Name) && Same(l.ToInput, input) && graph.RankOf(l.FromComponent) < graph.RankOf(component.Name));
                var state = new FlowInputState(input, capacity, incoming, crossesStages);
                inputs[(component.Name.ToUpperInvariant(), input.ToUpperInvariant())] = state;
                ports.Add(new FlowChannelInput(state));
            }
        }

        foreach (var component in graph.Components)
        {
            var errorOutputs = component is IFlowErrorOutputs errors ? errors.ErrorOutputs : [];
            foreach (var output in component.Outputs)
            {
                var tap = errorOutputs.Contains(output, StringComparer.OrdinalIgnoreCase) ? run._rejected : null;
                run._ports[component.Name].Add(new FlowChannelOutput(output, run.LinksOf(graph, component.Name, output, inputs), component.Name, tap, pauseGate));
            }
        }

        return run;
    }

    /// <summary>Gets the nodes of a component.</summary>
    /// <param name="component">The component's name.</param>
    /// <returns>Its input and output nodes in this run.</returns>
    public FlowPorts PortsOf(string component) => _ports[component];

    /// <summary>Records a failure; the first one recorded is the run's.</summary>
    /// <param name="component">The component that failed.</param>
    /// <param name="failure">The failure.</param>
    public void Fail(string component, IGenericResult failure) => Interlocked.CompareExchange(ref _stopped, new Stopped(component, failure), null);

    // Why one object: the failure and the component that caused it are set together by whichever component fails first.
    private sealed record Stopped(string Component, IGenericResult Failure);

    private List<FlowLinkState> LinksOf(
        FlowGraph graph, string component, string output, Dictionary<(string, string), FlowInputState> inputs)
    {
        var states = graph.Links
            .Where(l => Same(l.FromComponent, component) && Same(l.FromOutput, output))
            .Select(l => new FlowLinkState(l, inputs[(l.ToComponent.ToUpperInvariant(), l.ToInput.ToUpperInvariant())]))
            .ToList();
        if (states.Count == 0)
        {
            states.Add(new FlowLinkState(new FlowLink(component, output, string.Empty, string.Empty), null));
        }

        _links.AddRange(states);
        return states;
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
