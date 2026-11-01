using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Judges a flow graph before anything runs (FDW-1386).
/// </summary>
/// <remarks>
/// The rules, each refused with the name that breaks it: names are unique and stated; node names are unique within a component
/// and a component has at least one node; every link joins a real output to a real input; every input is fed (so a source, which
/// has none, takes nothing from any link); every output is linked or listed as dropped (never both); and there is no cycle.
/// Nothing is assumed: an unlinked node is a refusal, not an empty stream. <see cref="Problems"/> lists every rule broken, in the
/// order above; <see cref="Validate"/> refuses with the first.
/// </remarks>
public static class FlowGraphValidator
{
    /// <summary>Judges the graph.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success, or the failure naming the first rule the graph breaks.</returns>
    public static IGenericResult Validate(FlowGraph graph, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(logger);

        var problems = Problems(graph);
        return problems.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(FlowLog.GraphInvalid(logger, problems[0].Reason));
    }

    /// <summary>Lists every rule the graph breaks.</summary>
    /// <param name="graph">The graph.</param>
    /// <returns>The problems, in the order the rules are listed; empty when the graph can run.</returns>
    public static IReadOnlyList<FlowGraphProblem> Problems(FlowGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return [.. ComponentProblems(graph), .. LinkProblems(graph), .. NodeCoverageProblems(graph), .. CycleProblems(graph), .. StageProblems(graph)];
    }

    private static IEnumerable<FlowGraphProblem> ComponentProblems(FlowGraph graph)
    {
        if (graph.Components.Count == 0)
        {
            yield return new FlowGraphProblem("it has no components");
            yield break;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in graph.Components)
        {
            if (string.IsNullOrWhiteSpace(component.Name))
            {
                yield return new FlowGraphProblem("a component has no name");
                continue;
            }

            if (!seen.Add(component.Name))
            {
                yield return new FlowGraphProblem($"component name '{component.Name}' is used twice", component.Name);
            }

            foreach (var problem in ShapeProblems(component))
            {
                yield return problem;
            }
        }
    }

    private static IEnumerable<FlowGraphProblem> ShapeProblems(IFlowComponent component)
    {
        var duplicateInput = Duplicate(component.Inputs);
        if (duplicateInput is not null)
        {
            yield return new FlowGraphProblem(
                $"component '{component.Name}' declares the node name '{duplicateInput}' twice", component.Name, duplicateInput, "input");
        }

        var duplicateOutput = Duplicate(component.Outputs);
        if (duplicateOutput is not null)
        {
            yield return new FlowGraphProblem(
                $"component '{component.Name}' declares the node name '{duplicateOutput}' twice", component.Name, duplicateOutput, "output");
        }

        // What a component is follows from its nodes: no inputs makes a source, no outputs a destination. One with neither
        // moves nothing, which only an action or a loop does on purpose (FDW-2041): it runs as a step of the flow.
        if (component.Inputs.Count == 0 && component.Outputs.Count == 0 && component is not FlowActionComponent)
        {
            yield return new FlowGraphProblem($"component '{component.Name}' has no inputs and no outputs", component.Name);
        }
    }

    private static string? Duplicate(IReadOnlyList<string> names)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return names.FirstOrDefault(name => !seen.Add(name));
    }

    private static IEnumerable<FlowGraphProblem> LinkProblems(FlowGraph graph)
    {
        var byName = new Dictionary<string, IFlowComponent>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in graph.Components.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
        {
            byName.TryAdd(component.Name, component);
        }

        var seen = new HashSet<FlowLink>();
        for (var index = 0; index < graph.Links.Count; index++)
        {
            var link = graph.Links[index];
            var problem = SingleLinkProblem(byName, link, index);
            if (problem is not null)
            {
                yield return problem;
            }
            else if (!seen.Add(link))
            {
                yield return new FlowGraphProblem(
                    $"the link {Describe(link)} is given twice", link.FromComponent, link.FromOutput, "link", index);
            }
        }
    }

    private static FlowGraphProblem? SingleLinkProblem(Dictionary<string, IFlowComponent> byName, FlowLink link, int index)
    {
        if (!byName.TryGetValue(link.FromComponent, out var from))
        {
            return new FlowGraphProblem(
                $"the link {Describe(link)} starts at a component that does not exist", link.FromComponent, link.FromOutput, "link", index);
        }

        if (!byName.TryGetValue(link.ToComponent, out var to))
        {
            return new FlowGraphProblem(
                $"the link {Describe(link)} ends at a component that does not exist", link.FromComponent, link.FromOutput, "link", index);
        }

        if (!from.Outputs.Contains(link.FromOutput, StringComparer.OrdinalIgnoreCase))
        {
            return new FlowGraphProblem(
                $"the link {Describe(link)} starts at an output '{link.FromOutput}' that '{from.Name}' does not have",
                link.FromComponent, link.FromOutput, "link", index);
        }

        return to.Inputs.Contains(link.ToInput, StringComparer.OrdinalIgnoreCase)
            ? null
            : new FlowGraphProblem(
                $"the link {Describe(link)} ends at an input '{link.ToInput}' that '{to.Name}' does not have",
                link.FromComponent, link.FromOutput, "link", index);
    }

    // A stage names a component the graph has and is at least 1. A line may not run from a higher stage to a lower one, or from a component with a stage to
    // one with none (which runs first): the later stage would not have run yet when the earlier one needs its rows (FDW-2040).
    private static IEnumerable<FlowGraphProblem> StageProblems(FlowGraph graph)
    {
        foreach (var stage in graph.Stages)
        {
            if (!graph.Components.Any(component => Same(component.Name, stage.Component)))
            {
                yield return new FlowGraphProblem($"a stage is given to '{stage.Component}', which is not a component", stage.Component, "stage", "setting");
            }
            else if (stage.Stage < 1)
            {
                yield return new FlowGraphProblem($"the stage of '{stage.Component}' is {stage.Stage}; a stage is a number from 1", stage.Component, "stage", "setting");
            }
        }

        for (var index = 0; index < graph.Links.Count; index++)
        {
            var link = graph.Links[index];
            if (graph.RankOf(link.FromComponent) > graph.RankOf(link.ToComponent))
            {
                yield return new FlowGraphProblem(
                    $"the link {Describe(link)} runs from stage {Rank(graph, link.FromComponent)} to stage {Rank(graph, link.ToComponent)}, which runs first; a line may only go to the same or a later stage",
                    link.FromComponent, link.FromOutput, "link", index);
            }
        }
    }

    private static string Rank(FlowGraph graph, string component) =>
        graph.RankOf(component) == 0 ? "(none)" : graph.RankOf(component).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static IEnumerable<FlowGraphProblem> NodeCoverageProblems(FlowGraph graph)
    {
        foreach (var component in graph.Components)
        {
            foreach (var input in component.Inputs.Where(input => !graph.Links.Any(l => Same(l.ToComponent, component.Name) && Same(l.ToInput, input))))
            {
                yield return new FlowGraphProblem($"input '{input}' of '{component.Name}' is not linked to any output", component.Name, input, "input");
            }

            foreach (var output in component.Outputs)
            {
                var problem = OutputProblem(graph, component.Name, output);
                if (problem is not null)
                {
                    yield return problem;
                }
            }
        }

        foreach (var drop in graph.Dropped)
        {
            var component = graph.Components.FirstOrDefault(c => Same(c.Name, drop.Component));
            if (component is null || !component.Outputs.Contains(drop.Output, StringComparer.OrdinalIgnoreCase))
            {
                yield return new FlowGraphProblem(
                    $"dropped output '{drop.Output}' of '{drop.Component}' does not exist", drop.Component, drop.Output, "output");
            }
        }
    }

    private static FlowGraphProblem? OutputProblem(FlowGraph graph, string component, string output)
    {
        var linked = graph.Links.Any(l => Same(l.FromComponent, component) && Same(l.FromOutput, output));
        var dropped = graph.Dropped.Any(d => Same(d.Component, component) && Same(d.Output, output));
        if (linked && dropped)
        {
            return new FlowGraphProblem($"output '{output}' of '{component}' is both linked and dropped", component, output, "output");
        }

        return linked || dropped
            ? null
            : new FlowGraphProblem(
                $"output '{output}' of '{component}' is neither linked nor dropped; rows sent to it would be lost", component, output, "output");
    }

    // Kahn's algorithm: whatever cannot be taken off the front is in, or behind, a cycle.
    private static IEnumerable<FlowGraphProblem> CycleProblems(FlowGraph graph)
    {
        var names = graph.Components.Where(c => !string.IsNullOrWhiteSpace(c.Name)).Select(c => c.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var waiting = names.ToDictionary(n => n, _ => 0, StringComparer.OrdinalIgnoreCase);
        var real = graph.Links.Where(l => waiting.ContainsKey(l.FromComponent) && waiting.ContainsKey(l.ToComponent)).ToList();
        foreach (var link in real)
        {
            waiting[link.ToComponent]++;
        }

        var ready = new Queue<string>(waiting.Where(w => w.Value == 0).Select(w => w.Key));
        var removed = 0;
        while (ready.Count > 0)
        {
            var current = ready.Dequeue();
            removed++;
            foreach (var link in real.Where(l => Same(l.FromComponent, current)))
            {
                if (--waiting[link.ToComponent] == 0)
                {
                    ready.Enqueue(link.ToComponent);
                }
            }
        }

        if (removed == names.Count)
        {
            yield break;
        }

        var inCycle = waiting.Where(w => w.Value > 0).Select(w => w.Key).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        yield return new FlowGraphProblem(
            $"the links form a cycle through {string.Join(", ", inCycle.Select(n => $"'{n}'"))}", inCycle[0]);
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static string Describe(FlowLink link) =>
        $"'{link.FromComponent}.{link.FromOutput}' -> '{link.ToComponent}.{link.ToInput}'";
}
