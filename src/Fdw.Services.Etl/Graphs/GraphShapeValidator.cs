using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Judges a stored graph without running it or reaching any data (FDW-1396, FDW-1403, FDW-2041): its variables and components, every
/// component's settings against its kind, and, for the top level and for the body of each loop, the shape of the flow by the engine's own
/// graph check.
/// </summary>
/// <remarks>
/// What it cannot judge is what only the data can say: that a data set exists, that a column is in it. It refuses on the first
/// problem it finds and names the component and the setting.
/// </remarks>
public static class GraphShapeValidator
{
    /// <summary>Lists every rule the graph breaks.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="logger">The logger a condition's refusal is written to.</param>
    /// <returns>The problems, each with its path; empty when the graph is sound.</returns>
    public static IReadOnlyList<GraphProblem> Problems(GraphPipelineConfiguration graph, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(logger);
        if (graph.Components.Count == 0)
        {
            return [new GraphProblem([], $"graph pipeline '{graph.Name}' has no components, so there is nothing to run")];
        }

        var problems = new List<GraphProblem>();
        if (graph.LinkCapacity < 1)
        {
            problems.Add(new GraphProblem(["linkCapacity"], $"graph pipeline '{graph.Name}' does not state how many batches a link buffers ({graph.LinkCapacity} is not at least one)"));
        }

        if (graph.MaxParallelRuns < 1)
        {
            problems.Add(new GraphProblem(["maxParallelRuns"], $"graph pipeline '{graph.Name}' does not state how many of its runs may run at once ({graph.MaxParallelRuns} is not at least one)"));
        }

        if (ReferenceEquals(OverflowBehaviors.ByName(graph.OverflowBehavior), OverflowBehaviors.NotFound))
        {
            problems.Add(new GraphProblem(["overflowBehavior"], $"graph pipeline '{graph.Name}' names the overflow behavior '{graph.OverflowBehavior}', which is not one of {nameof(OverflowBehaviors)}"));
        }

        problems.AddRange(DelayedStartProblems(graph));
        problems.AddRange(LinkEndProblems(graph));
        var broken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        problems.AddRange(GraphComponentValidator.Problems(graph, broken, logger));
        foreach (var level in Levels(graph))
        {
            problems.AddRange(LevelProblems(graph, level, broken, logger));
        }

        return problems;
    }

    // The delay or time of day of an overflow behavior that holds a run (FDW-3129): a behavior that holds states exactly one of them, with a
    // time zone for a time of day; any other behavior states none. An unknown behavior is reported by the caller and judged by nothing here.
    private static IEnumerable<GraphProblem> DelayedStartProblems(GraphPipelineConfiguration graph)
    {
        var behavior = OverflowBehaviors.ByName(graph.OverflowBehavior);
        if (ReferenceEquals(behavior, OverflowBehaviors.NotFound))
        {
            return [];
        }

        var found = behavior.HoldsWhenFull
            ? PipelineDelayedStart.TryRead(graph.DelayedStartSeconds, graph.DelayedStartTimeOfDay, graph.DelayedStartTimeZoneId, out _)
            : PipelineDelayedStart.Unexpected(graph.DelayedStartSeconds, graph.DelayedStartTimeOfDay, graph.DelayedStartTimeZoneId, behavior.Name);
        return found.Select(problem => new GraphProblem([problem.Field], $"graph pipeline '{graph.Name}': {problem.Message}"));
    }

    // A line whose end names a component the graph does not have. No level can judge it: a level takes the lines with an end among its own
    // components, and a line to a component that is nowhere is not a line between two components of one place.
    private static IEnumerable<GraphProblem> LinkEndProblems(GraphPipelineConfiguration graph)
    {
        var names = new HashSet<string>(graph.Components.Select(component => component.Name), StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < graph.Links.Count; index++)
        {
            var link = graph.Links[index];
            string[] path = ["links", index.ToString(CultureInfo.InvariantCulture)];
            if (!names.Contains(link.FromComponent))
            {
                yield return new GraphProblem(path, $"the line to '{link.ToComponent}' starts at '{link.FromComponent}', which does not exist in graph pipeline '{graph.Name}'");
            }

            if (!names.Contains(link.ToComponent))
            {
                yield return new GraphProblem(path, $"the line from '{link.FromComponent}' ends at '{link.ToComponent}', which does not exist in graph pipeline '{graph.Name}'");
            }
        }
    }

    // The top level and the body of each loop: the components that share a parent. A loop that is itself broken is not looked into twice.
    private static IEnumerable<string?> Levels(GraphPipelineConfiguration graph)
    {
        yield return null;
        foreach (var loop in graph.Components.Where(component => ComponentTypes.ByName(component.ComponentType).HoldsComponents))
        {
            yield return loop.Name;
        }
    }

    // One level: the shape of its flow by the engine's own graph check, over components that have only their nodes, and a line that leaves
    // the level is refused (a loop's body runs again each round, so its lines cannot reach outside it).
    private static IEnumerable<GraphProblem> LevelProblems(GraphPipelineConfiguration graph, string? parent, HashSet<string> broken, ILogger logger)
    {
        var members = graph.Components.Where(component => string.Equals(component.ParentComponent, parent, StringComparison.OrdinalIgnoreCase)).ToList();
        if (members.Count == 0)
        {
            yield break;
        }

        var inLevel = new HashSet<string>(members.Select(member => member.Name), StringComparer.OrdinalIgnoreCase);
        var links = new List<PipelineLinkConfiguration>();
        for (var index = 0; index < graph.Links.Count; index++)
        {
            var link = graph.Links[index];
            var fromHere = inLevel.Contains(link.FromComponent);
            var toHere = inLevel.Contains(link.ToComponent);
            if (fromHere && toHere)
            {
                links.Add(link);
            }
            else if (fromHere && graph.Components.Any(component => string.Equals(component.Name, link.ToComponent, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new GraphProblem(
                    ["links", index.ToString(CultureInfo.InvariantCulture)],
                    $"the line from '{link.FromComponent}' to '{link.ToComponent}' joins components that are not in the same place: one is in the body of a loop and the other is not, or they are in different loops");
            }
        }

        // Why only the components that move rows: an action or a loop has no nodes, so it is nothing for the flow check to judge.
        var movers = members.Where(component => !ComponentTypes.ByName(component.ComponentType).IsAction).ToList();
        if (movers.Count == 0)
        {
            yield break;
        }

        var shapes = movers
            .Select(component =>
            {
                var (inputs, outputs) = ComponentShapes.Of(component, links);
                return (IFlowComponent)new ShapeFlowComponent(component.Name, inputs, outputs, logger);
            })
            .ToList();
        var flow = new FlowGraph(
            shapes,
            [.. links.Select(link => new FlowLink(link.FromComponent, link.FromOutput, link.ToComponent, link.ToInput))],
            [.. graph.DroppedOutputs.Where(output => inLevel.Contains(output.ComponentName)).Select(output => new FlowDroppedOutput(output.ComponentName, output.OutputName))],
            ComponentStages.Of(movers));
        foreach (var problem in FlowGraphValidator.Problems(flow).Where(problem => problem.Component is null || !broken.Contains(problem.Component)))
        {
            yield return new GraphProblem(PathOf(problem, graph.Links, links), problem.Reason);
        }
    }

    /// <summary>Judges a stored graph.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the first refusal.</returns>
    public static IGenericResult Validate(GraphPipelineConfiguration graph, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(logger);
        var problems = Problems(graph, logger);
        return problems.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(FlowLog.GraphInvalid(logger, problems[0].Reason));
    }

    // A link's index in a level's problem is its place in the level's own list; the path names its place in the graph's.
    private static IReadOnlyList<string> PathOf(FlowGraphProblem problem, IList<PipelineLinkConfiguration> all, List<PipelineLinkConfiguration> level)
    {
        if (problem.LinkIndex is { } index)
        {
            return ["links", all.IndexOf(level[index]).ToString(CultureInfo.InvariantCulture)];
        }

        if (problem.Component is null)
        {
            return [];
        }

        if (problem.Node is null || problem.NodeKind is null)
        {
            return ["components", problem.Component];
        }

        return string.Equals(problem.NodeKind, "setting", StringComparison.Ordinal)
            ? ["components", problem.Component, "settings", problem.Node]
            : ["components", problem.Component, string.Equals(problem.NodeKind, "input", StringComparison.Ordinal) ? "inputs" : "outputs", problem.Node];
    }
}
