using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A live view that also reads what each component of a graph holds in memory (FDW-5231). The executor opens a graph through this when the view offers it, and
/// through <see cref="IFlowLiveRun.Open"/> otherwise.
/// </summary>
public interface IFlowLiveHeldRun : IFlowLiveRun
{
    /// <summary>Opens a graph that is about to run, with what each of its components holds.</summary>
    /// <param name="components">The graph's components with their stages.</param>
    /// <param name="links">Reads what every link of the graph has carried so far; called only when the view is read.</param>
    /// <param name="held">Reads what a component, by name, holds now and the most it has held; called only when the view is read.</param>
    /// <returns>The graph, to tell as components start and end and to dispose when the graph ends.</returns>
    IFlowLiveGraph Open(IReadOnlyList<FlowLiveComponent> components, Func<IReadOnlyList<FlowLinkCount>> links, Func<string, FlowHeldCount> held);
}
