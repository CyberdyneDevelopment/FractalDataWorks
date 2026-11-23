using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// What a stored graph says about its decisions (FDW-6134): the outputs a decision component fires from, the output a loop takes as its test, and the decisions that test depends on.
/// One place reads them, so the builder and the save-time check cannot differ.
/// </summary>
public static class DecisionTests
{
    /// <summary>Reads the text a setting of a component is stated with, from the column that holds it (FDW-7275).</summary>
    /// <param name="component">The stored component.</param>
    /// <param name="name">The setting's name.</param>
    /// <returns>The text, a Boolean as <c>True</c> or <c>False</c>; <see langword="null"/> when the component does not state the setting or states it blank.</returns>
    public static string? StatedText(PipelineComponentConfiguration component, string name) =>
        ComponentSettingBindings.Find(name)?.Get(component) switch
        {
            bool flag => flag.ToString(),
            string text when !string.IsNullOrWhiteSpace(text) => text.Trim(),
            _ => null,
        };

    /// <summary>Reads the output a loop takes as its test.</summary>
    /// <param name="loop">The stored loop.</param>
    /// <returns>The setting's text, <c>component.output</c>, or <see langword="null"/> when the loop states no testOutput.</returns>
    public static string? TestOutputOf(PipelineComponentConfiguration loop) => StatedText(loop, DoWhileComponentType.TestOutputSetting);

    /// <summary>Lists the outputs a stored decision fires from.</summary>
    /// <param name="component">The stored component.</param>
    /// <returns>The outputs; empty for a component that is not a decision.</returns>
    public static IReadOnlyList<string> OutputsOf(PipelineComponentConfiguration component) =>
        ComponentTypes.ByName(component.ComponentType) is IDecisionComponentType decision ? decision.FiredOutputs(name => StatedText(component, name)) : [];

    /// <summary>Lists the decisions a loop's test depends on: the decision it names and every decision whose output feeds one of them, inside the loop.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="loop">The loop's name.</param>
    /// <param name="decision">The decision the test names.</param>
    /// <returns>The names of the decisions, the one named included.</returns>
    public static IReadOnlySet<string> ConeOf(IGraphPipelineConfiguration graph, string loop, string decision)
    {
        var cone = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { decision };
        var pending = new Queue<string>([decision]);
        while (pending.TryDequeue(out var current))
        {
            foreach (var link in graph.Links.Where(link => string.Equals(link.ToComponent, current, StringComparison.OrdinalIgnoreCase)))
            {
                var from = graph.Components.FirstOrDefault(component => string.Equals(component.Name, link.FromComponent, StringComparison.OrdinalIgnoreCase));
                if (from is not null && string.Equals(from.ParentComponent, loop, StringComparison.OrdinalIgnoreCase) && CompletionLines.IsSignal(link, graph.Components) && cone.Add(from.Name))
                {
                    pending.Enqueue(from.Name);
                }
            }
        }

        return cone;
    }
}
