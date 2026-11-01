using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Works out which input and output nodes a stored component has (FDW-1396), from what its kind says in the catalogue and what the
/// component and the graph's links state.
/// </summary>
/// <remarks>
/// A node the kind names is always there. A node the person building the graph names is there when its setting names it (a
/// split's filtered outputs, an error or unmatched output) or, when no setting names it, when a link uses it (a union's inputs).
/// This is the one place that rule is written, so the check of a graph and a description of its nodes cannot disagree.
/// </remarks>
public static class ComponentShapes
{
    /// <summary>Lists the input and output nodes of a stored component.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="links">The graph's links, which name the nodes no setting names.</param>
    /// <returns>The node names in catalogue order, each once; no nodes when the component's kind is not in the catalogue.</returns>
    public static (IReadOnlyList<string> Inputs, IReadOnlyList<string> Outputs) Of(
        PipelineComponentConfiguration component, IReadOnlyList<PipelineLinkConfiguration> links)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(links);
        var kind = ComponentTypes.ByName(component.ComponentType);
        if (ReferenceEquals(kind, ComponentTypes.NotFound))
        {
            return ([], []);
        }

        var fromLinks = links.Where(link => string.Equals(link.ToComponent, component.Name, StringComparison.OrdinalIgnoreCase))
            .Select(link => link.ToInput);
        var toLinks = links.Where(link => string.Equals(link.FromComponent, component.Name, StringComparison.OrdinalIgnoreCase))
            .Select(link => link.FromOutput);
        return (Names(component, kind.Inputs, fromLinks), Names(component, kind.Outputs, toLinks));
    }

    /// <summary>Lists the output nodes of a stored component that carry the rows it rejects (FDW-2041), so a component built later can still be told apart in the run.</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="links">The links of the level the component is in.</param>
    /// <returns>The names of the kind's error outputs, resolved as <see cref="Of"/> resolves its outputs.</returns>
    public static IReadOnlyList<string> ErrorOutputsOf(PipelineComponentConfiguration component, IReadOnlyList<PipelineLinkConfiguration> links)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(links);
        var kind = ComponentTypes.ByName(component.ComponentType);
        if (ReferenceEquals(kind, ComponentTypes.NotFound))
        {
            return [];
        }

        var toLinks = links.Where(link => string.Equals(link.FromComponent, component.Name, StringComparison.OrdinalIgnoreCase))
            .Select(link => link.FromOutput);
        return Names(component, [.. kind.Outputs.Where(spec => spec.IsError)], toLinks);
    }

    private static IReadOnlyList<string> Names(
        PipelineComponentConfiguration component, IReadOnlyList<FlowNodeSpec> specs, IEnumerable<string> linked)
    {
        var names = new List<string>();
        foreach (var spec in specs)
        {
            if (!spec.IsNamedByUser)
            {
                names.Add(spec.Name);
            }
            else if (spec.NamedBySetting is { } setting)
            {
                names.AddRange(NamedBy(component, setting));
            }
            else
            {
                names.AddRange(linked);
            }
        }

        return [.. names.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static IEnumerable<string> NamedBy(PipelineComponentConfiguration component, string setting)
    {
        if (string.Equals(setting, "outputs", StringComparison.OrdinalIgnoreCase))
        {
            return component.SplitOutputs.OrderBy(output => output.Ordinal).Select(output => output.OutputName);
        }

        return ComponentSettingBindings.Find(setting)?.Get(component) is string name ? [name] : [];
    }
}
