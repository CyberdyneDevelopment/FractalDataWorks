using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Components and the links between them: what a run executes (FDW-1386).
/// </summary>
public sealed class FlowGraph
{
    /// <summary>Initializes a new instance of the <see cref="FlowGraph"/> class.</summary>
    /// <param name="components">The components.</param>
    /// <param name="links">The links between their nodes.</param>
    /// <param name="dropped">The output nodes that are deliberately linked to nothing.</param>
    public FlowGraph(
        IReadOnlyList<IFlowComponent> components,
        IReadOnlyList<FlowLink> links,
        IReadOnlyList<FlowDroppedOutput> dropped)
        : this(components, links, dropped, [])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FlowGraph"/> class with components that run in stages (FDW-2040).</summary>
    /// <param name="components">The components.</param>
    /// <param name="links">The links between their nodes.</param>
    /// <param name="dropped">The output nodes that are deliberately linked to nothing.</param>
    /// <param name="stages">The stage of each component that has one; a component not listed has none.</param>
    public FlowGraph(
        IReadOnlyList<IFlowComponent> components,
        IReadOnlyList<FlowLink> links,
        IReadOnlyList<FlowDroppedOutput> dropped,
        IReadOnlyList<FlowStage> stages)
    {
        Components = components ?? throw new ArgumentNullException(nameof(components));
        Links = links ?? throw new ArgumentNullException(nameof(links));
        Dropped = dropped ?? throw new ArgumentNullException(nameof(dropped));
        Stages = stages ?? throw new ArgumentNullException(nameof(stages));
    }

    /// <summary>
    /// Gets the stage of each component that has one. Components with no stage run first and together; then stage 1 runs, and when every component of a
    /// stage has finished the next higher stage runs.
    /// </summary>
    public IReadOnlyList<FlowStage> Stages { get; }

    /// <summary>
    /// Says what rank a component runs at: 0 for a component with no stage, which runs before every stage, and its stage number otherwise.
    /// </summary>
    /// <param name="component">The component's name.</param>
    /// <returns>The rank; a link from a higher rank to a lower one could never be read in time.</returns>
    public int RankOf(string component) =>
        Stages.FirstOrDefault(stage => string.Equals(stage.Component, component, StringComparison.OrdinalIgnoreCase))?.Stage ?? 0;

    /// <summary>Gets the components.</summary>
    public IReadOnlyList<IFlowComponent> Components { get; }

    /// <summary>Gets the links between the components' nodes.</summary>
    public IReadOnlyList<FlowLink> Links { get; }

    /// <summary>Gets the output nodes that are deliberately linked to nothing.</summary>
    public IReadOnlyList<FlowDroppedOutput> Dropped { get; }
}
