using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Tells the lines of a stored graph apart (FDW-3640, FDW-6134): a line that leaves a component's <c>done</c> output is a completion line, and a line that leaves an output of a
/// decision (a condition, a gate, a switch) is a signal line; neither carries rows. Every other line is a row line. Every reader of the stored lines that means rows reads
/// them through here, so none mistakes a completion or a signal line for a row node.
/// </summary>
/// <remarks>
/// Why the components are asked for (FDW-6134): a decision's outputs are named by its kind and, for a switch, by the person, so the name of an output cannot say that a line is a signal;
/// the kind of the component the line leaves can.
/// </remarks>
public static class CompletionLines
{
    /// <summary>Says whether a stored line leaves an output of a decision.</summary>
    /// <param name="link">The stored line.</param>
    /// <param name="components">The components of the graph.</param>
    /// <returns><see langword="true"/> when the component the line leaves is of a decision kind.</returns>
    public static bool IsSignal(PipelineLinkConfiguration link, IEnumerable<PipelineComponentConfiguration> components)
    {
        var from = components.FirstOrDefault(component => string.Equals(component.Name, link.FromComponent, StringComparison.OrdinalIgnoreCase));
        return from is not null && ComponentTypes.ByName(from.ComponentType) is IDecisionComponentType;
    }

    /// <summary>Says whether a stored line is a completion line or a signal line, which is to say that it carries no rows.</summary>
    /// <param name="link">The stored line.</param>
    /// <param name="components">The components of the graph.</param>
    /// <returns><see langword="true"/> when it leaves a <c>done</c> output or an output of a decision.</returns>
    public static bool Is(PipelineLinkConfiguration link, IEnumerable<PipelineComponentConfiguration> components)
    {
        return CompletionNodes.IsDone(link.FromOutput) || IsSignal(link, components);
    }

    /// <summary>Lists the lines that carry rows.</summary>
    /// <param name="links">The stored lines.</param>
    /// <param name="components">The components of the graph.</param>
    /// <returns>The row lines, in order.</returns>
    public static IReadOnlyList<PipelineLinkConfiguration> Rows(IEnumerable<PipelineLinkConfiguration> links, IEnumerable<PipelineComponentConfiguration> components)
    {
        return [.. links.Where(link => !Is(link, components))];
    }

    /// <summary>Lists the lines that order two components: the completion lines and the signal lines.</summary>
    /// <param name="links">The stored lines.</param>
    /// <param name="components">The components of the graph.</param>
    /// <returns>The lines, in order.</returns>
    public static IReadOnlyList<PipelineLinkConfiguration> Completions(IEnumerable<PipelineLinkConfiguration> links, IEnumerable<PipelineComponentConfiguration> components)
    {
        return [.. links.Where(link => Is(link, components))];
    }

    /// <summary>Lists the signal lines.</summary>
    /// <param name="links">The stored lines.</param>
    /// <param name="components">The components of the graph.</param>
    /// <returns>The signal lines, in order.</returns>
    public static IReadOnlyList<PipelineLinkConfiguration> Signals(IEnumerable<PipelineLinkConfiguration> links, IEnumerable<PipelineComponentConfiguration> components)
    {
        return [.. links.Where(link => IsSignal(link, components))];
    }

    /// <summary>Lists the decision outputs whose lines lead to a component, as the flow form of them.</summary>
    /// <param name="links">The stored lines.</param>
    /// <param name="components">The components of the graph.</param>
    /// <param name="component">The component the lines lead to.</param>
    /// <returns>The outputs, in the order of the lines.</returns>
    public static IReadOnlyList<FlowSignal> SignalsInto(IEnumerable<PipelineLinkConfiguration> links, IEnumerable<PipelineComponentConfiguration> components, string component)
    {
        return [.. links
            .Where(link => IsSignal(link, components) && string.Equals(link.ToComponent, component, StringComparison.OrdinalIgnoreCase))
            .Select(link => new FlowSignal(link.FromComponent, link.FromOutput))];
    }

    /// <summary>Makes the flow form of a stored completion or signal line.</summary>
    /// <param name="link">The stored line.</param>
    /// <returns>The component that must finish and the one that waits for it.</returns>
    public static FlowCompletion ToCompletion(PipelineLinkConfiguration link)
    {
        return new FlowCompletion(link.FromComponent, link.ToComponent);
    }
}
