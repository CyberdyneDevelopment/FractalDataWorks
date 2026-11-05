using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The graphs one pipeline run is running now (FDW-3382): usually one, more while a loop runs its body or runs bodies side by side. Disposing it ends the run's place in the live view.
/// </summary>
public sealed class RunLive : IFlowLiveRun, IDisposable
{
    private readonly Lock _gate = new();
    private readonly List<LiveGraph> _graphs = [];
    private readonly Action _closed;

    internal RunLive(Action closed)
    {
        _closed = closed ?? throw new ArgumentNullException(nameof(closed));
    }

    /// <inheritdoc />
    public IFlowLiveGraph Open(IReadOnlyList<FlowLiveComponent> components, Func<IReadOnlyList<FlowLinkCount>> links)
    {
        var graph = new LiveGraph(components, links, Remove);
        lock (_gate)
        {
            _graphs.Add(graph);
        }

        return graph;
    }

    /// <inheritdoc />
    public void Dispose() => _closed();

    /// <summary>Reads the components of every graph the run is running now.</summary>
    /// <returns>The components, graph by graph in the order the graphs opened.</returns>
    internal IReadOnlyList<FlowLiveComponentView> View()
    {
        LiveGraph[] graphs;
        lock (_gate)
        {
            graphs = [.. _graphs];
        }

        return [.. graphs.SelectMany(graph => graph.View())];
    }

    internal IReadOnlyList<FlowLinkCount> Links()
    {
        LiveGraph[] graphs;
        lock (_gate)
        {
            graphs = [.. _graphs];
        }

        return [.. graphs.SelectMany(graph => graph.Links())];
    }

    private void Remove(LiveGraph graph)
    {
        lock (_gate)
        {
            _ = _graphs.Remove(graph);
        }
    }
}
