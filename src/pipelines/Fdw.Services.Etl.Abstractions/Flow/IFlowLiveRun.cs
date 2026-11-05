using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Where the executor opens a flow graph for the live view (FDW-3382): one pipeline run, whatever graphs it runs in turn or at once.
/// </summary>
public interface IFlowLiveRun
{
    /// <summary>Opens a graph that is about to run.</summary>
    /// <param name="components">The graph's components with their stages.</param>
    /// <param name="links">Reads what every link of the graph has carried so far; called only when the view is read, never by the components.</param>
    /// <returns>The graph, to tell as components start and end and to dispose when the graph ends.</returns>
    IFlowLiveGraph Open(IReadOnlyList<FlowLiveComponent> components, Func<IReadOnlyList<FlowLinkCount>> links);
}
