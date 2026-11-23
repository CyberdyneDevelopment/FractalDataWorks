using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// How deep a stored graph nests the components that hold other components (FDW-6134): a container, a loop, or a kind another package makes that holds components
/// (<see cref="IComponentType.HoldsComponents"/>). A holder at the top level is one level deep; a holder inside it is two.
/// </summary>
public static class GraphContainerDepth
{
    /// <summary>Finds the deepest component that holds components.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <returns>The innermost holder and its depth; no holder and zero when the graph nests none.</returns>
    public static (string? Component, int Depth) Deepest(IGraphPipelineConfiguration graph)
    {
        string? deepest = null;
        var most = 0;
        foreach (var component in graph.Components.Where(IsHolder))
        {
            var depth = DepthOf(graph, component);
            if (depth > most)
            {
                most = depth;
                deepest = component.Name;
            }
        }

        return (deepest, most);
    }

    private static bool IsHolder(PipelineComponentConfiguration component) => ComponentTypes.ByName(component.ComponentType).HoldsComponents;

    // The holders from the component up to the top, itself included. A parent chain that leads back to a component already seen ends there: that graph is refused by its own check.
    private static int DepthOf(IGraphPipelineConfiguration graph, PipelineComponentConfiguration holder)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { holder.Name };
        var depth = 1;
        var parent = holder.ParentComponent;
        while (parent is not null && seen.Add(parent))
        {
            var found = graph.Components.FirstOrDefault(component => string.Equals(component.Name, parent, StringComparison.OrdinalIgnoreCase));
            if (found is null)
            {
                break;
            }

            if (IsHolder(found))
            {
                depth++;
            }

            parent = found.ParentComponent;
        }

        return depth;
    }
}
