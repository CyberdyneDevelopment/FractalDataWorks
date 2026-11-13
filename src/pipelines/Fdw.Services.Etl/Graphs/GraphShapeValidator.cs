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
    public static IReadOnlyList<GraphProblem> Problems(GraphPipelineConfiguration graph, ILogger logger) => Problems(graph, null, logger);

    /// <summary>Lists every rule a graph that is read and not loaded breaks (FDW-5291).</summary>
    /// <remarks>
    /// The end's output is judged as the read builds it: consumed by the read's own component (<see cref="GraphEnd.ToReader"/>), so it is neither unlinked
    /// nor lost. An end that names a component that is not a top-level component of the graph is a problem of its own.
    /// </remarks>
    /// <param name="graph">The stored graph.</param>
    /// <param name="end">The output the read takes the rows of; none for a graph that is run and not read.</param>
    /// <param name="logger">The logger a condition's refusal is written to.</param>
    /// <returns>The problems, each with its path; empty when the graph is sound.</returns>
    public static IReadOnlyList<GraphProblem> Problems(GraphPipelineConfiguration graph, GraphEnd? end, ILogger logger)
    {
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
        problems.AddRange(EndProblems(graph, end));
        var broken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        problems.AddRange(GraphComponentValidator.Problems(graph, broken, logger));
        foreach (var level in Levels(graph))
        {
            problems.AddRange(LevelProblems(graph, level, level is null ? end : null, broken, logger));
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
    private static IEnumerable<GraphProblem> LevelProblems(GraphPipelineConfiguration graph, string? parent, GraphEnd? end, HashSet<string> broken, ILogger logger)
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

        // Why every member: a completion line can start or end at an action or a loop, which has no row nodes, so the lines are judged over all of them (FDW-3640).
        foreach (var problem in CompletionProblems(graph, members, links, broken))
        {
            yield return problem;
        }

        // Why only the components that move rows: an action or a loop has no row nodes, so it is nothing for the flow check to judge.
        var movers = members.Where(component => !ComponentTypes.ByName(component.ComponentType).IsAction).ToList();
        if (movers.Count == 0)
        {
            yield break;
        }

        var rows = CompletionLines.Rows(links);
        foreach (var problem in ReservedNameProblems(movers, rows))
        {
            yield return problem;
        }

        foreach (var problem in FlowShapeProblems(graph, movers, rows, inLevel, end, broken, logger))
        {
            yield return problem;
        }
    }

    // The engine's own graph check over the components that move rows and the lines that carry rows.
    private static IEnumerable<GraphProblem> FlowShapeProblems(
        GraphPipelineConfiguration graph,
        IReadOnlyList<PipelineComponentConfiguration> movers,
        IReadOnlyList<PipelineLinkConfiguration> rows,
        HashSet<string> inLevel,
        GraphEnd? end,
        HashSet<string> broken,
        ILogger logger)
    {
        var shapes = movers
            .Select(component =>
            {
                var (inputs, outputs) = ComponentShapes.Of(component, rows);
                return (IFlowComponent)new ShapeFlowComponent(component.Name, inputs, outputs, logger);
            })
            .ToList();
        var flow = new FlowGraph(
            WithReader(shapes, end, inLevel, logger),
            WithReaderLink([.. rows.Select(link => new FlowLink(link.FromComponent, link.FromOutput, link.ToComponent, link.ToInput))], end, inLevel),
            [.. graph.DroppedOutputs.Where(output => inLevel.Contains(output.ComponentName)).Select(output => new FlowDroppedOutput(output.ComponentName, output.OutputName))],
            ComponentStages.Of(movers));
        foreach (var problem in FlowGraphValidator.Problems(flow).Where(problem => problem.Component is null || !broken.Contains(problem.Component)))
        {
            // Why the read's own line has its own path: it is added after the stored lines (WithReaderLink), so its index is one past them and no stored line is at it; the line is the stated fields out, named at fieldsOutOutput.
            yield return new GraphProblem(
                end is not null && inLevel.Contains(end.Component) && problem.LinkIndex == rows.Count ? ["fieldsOutOutput"] : PathOf(problem, graph.Links, rows),
                problem.Reason);
        }
    }

    // Why the read's own component is judged here: the read adds it after the end (GraphFlowBuilder.BuildRead), so the check judges the flow the read builds. Only an
    // end among the components of the level counts; one that names a component elsewhere is a problem of its own (EndProblems).
    private static List<IFlowComponent> WithReader(List<IFlowComponent> shapes, GraphEnd? end, HashSet<string> inLevel, ILogger logger) =>
        end is not null && inLevel.Contains(end.Component)
            ? [.. shapes, new ShapeFlowComponent(GraphEnd.ReaderName, [FlowDestination.InputName], [], logger)]
            : shapes;

    private static List<FlowLink> WithReaderLink(List<FlowLink> rows, GraphEnd? end, HashSet<string> inLevel) =>
        end is not null && inLevel.Contains(end.Component) ? [.. rows, end.ToReader()] : rows;

    // An end whose component is not a top-level component of the graph: the read adds its component at the top level, so a component in a loop's body, or none, has no
    // output for it to take.
    private static IEnumerable<GraphProblem> EndProblems(GraphPipelineConfiguration graph, GraphEnd? end)
    {
        if (end is not null && !graph.Components.Any(component => component.ParentComponent is null && string.Equals(component.Name, end.Component, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new GraphProblem(["fieldsOutComponent"], $"the fields out come from component '{end.Component}', which is not a component of graph '{graph.Name}'");
        }
    }

    // The completion lines of a level (FDW-3640): judged by the validator the run also uses, over every member, so a line from or to an action is covered.
    private static IEnumerable<GraphProblem> CompletionProblems(
        GraphPipelineConfiguration graph, IReadOnlyList<PipelineComponentConfiguration> members, IReadOnlyList<PipelineLinkConfiguration> links, HashSet<string> broken)
    {
        var completions = CompletionLines.Completions(links);
        if (completions.Count == 0)
        {
            yield break;
        }

        var stages = ComponentStages.Of(members);
        var found = FlowCompletionValidator.Problems(
            [.. members.Select(member => member.Name)],
            [.. CompletionLines.Rows(links).Select(link => new FlowLink(link.FromComponent, link.FromOutput, link.ToComponent, link.ToInput))],
            [.. completions.Select(CompletionLines.ToCompletion)],
            name => FlowStage.RankIn(stages, name));
        foreach (var problem in found.Where(problem => problem.Component is null || !broken.Contains(problem.Component)))
        {
            yield return new GraphProblem(
                problem.LinkIndex is { } index
                    ? ["links", graph.Links.IndexOf(completions[index]).ToString(CultureInfo.InvariantCulture)]
                    : ["components", problem.Component ?? string.Empty],
                problem.Reason);
        }
    }

    // A row output a person names done would read as a completion output (FDW-3640): the name is refused, naming the component.
    private static IEnumerable<GraphProblem> ReservedNameProblems(
        IReadOnlyList<PipelineComponentConfiguration> movers, IReadOnlyList<PipelineLinkConfiguration> rows) =>
        movers
            .Where(component => ComponentShapes.Of(component, rows).Outputs.Any(CompletionNodes.IsDone))
            .Select(component => new GraphProblem(
                ["components", component.Name],
                $"component '{component.Name}' has an output named '{CompletionNodes.Done}', which is the name every component's completion output has; give the output another name"));

    /// <summary>Judges a stored graph.</summary>
    /// <param name="graph">The stored graph.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the first refusal.</returns>
    public static IGenericResult Validate(GraphPipelineConfiguration graph, ILogger logger)
    {
        var problems = Problems(graph, logger);
        return problems.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(FlowLog.GraphInvalid(logger, problems[0].Reason));
    }

    // A link's index in a level's problem is its place in the level's own list; the path names its place in the graph's.
    private static IReadOnlyList<string> PathOf(FlowGraphProblem problem, IList<PipelineLinkConfiguration> all, IReadOnlyList<PipelineLinkConfiguration> level)
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
