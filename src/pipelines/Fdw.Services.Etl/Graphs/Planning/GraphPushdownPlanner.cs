using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// Plans where each join, union and lookup of a graph runs (FDW-6969): one whose inputs read, through container sources and plain renames, one data
/// store runs as one query in that store; everything else runs in memory, and the trace says why. The planner never guesses: what it cannot state
/// in the store stays in memory.
/// </summary>
/// <remarks>
/// Every combining component at the top level is tried as the top of a pushed sub-graph. One that a pushed component above it reads as its left side is
/// not a top of its own: the component above stands in for it. A failure to describe a container is a reason to read it in memory, never a failure of the run.
/// </remarks>
public static class GraphPushdownPlanner
{
    /// <summary>Plans a graph.</summary>
    /// <param name="graph">The graph.</param>
    /// <param name="gateway">The data gateway its sources read through.</param>
    /// <param name="end">Where a read takes its rows; null when the graph is not read.</param>
    /// <param name="logger">The logger the trace goes to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The plan.</returns>
    public static async Task<GraphPushdownPlan> Plan(
        IGraphPipelineConfiguration graph, IDataGateway gateway, GraphEnd? end, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (gateway is not IPushdownPlanningGateway planning)
        {
            PushdownLog.PlanningNotOffered(logger, graph.Name);
            return GraphPushdownPlan.None;
        }

        var scope = new PushdownScope(graph, planning, logger, end?.Component);
        var tried = new List<(PipelineComponentConfiguration Component, IPushdownPart Part, PushdownAttempt Attempt)>();
        foreach (var component in graph.Components.Where(candidate => candidate.ParentComponent is null))
        {
            var part = PushdownParts.ByName(component.ComponentType);
            if (!ReferenceEquals(part, PushdownParts.NotFound) && part.Combines)
            {
                tried.Add((component, part, await part.Resolve(scope, component, true, cancellationToken).ConfigureAwait(false)));
            }
        }

        var absorbed = new HashSet<string>(
            tried.SelectMany(entry => entry.Attempt.Side is { } side
                ? side.Members.Where(member => !string.Equals(member, entry.Component.Name, StringComparison.OrdinalIgnoreCase))
                : []),
            StringComparer.OrdinalIgnoreCase);
        var tops = tried.Where(entry => !absorbed.Contains(entry.Component.Name)).ToList();
        var (pushed, inMemory) = Report(graph, tops, logger);

        // Why the combiners left in memory are read again (FDW-6970): one whose inputs read more than one data store reads them under the federation strategy.
        var federated = await PushdownFederation.Reads(
            scope,
            [.. tops.Where(entry => inMemory.Exists(left => string.Equals(left.Component, entry.Component.Name, StringComparison.OrdinalIgnoreCase)))
                .Select(entry => (entry.Component, entry.Part))],
            pushed,
            cancellationToken).ConfigureAwait(false);
        return new GraphPushdownPlan(pushed, inMemory, federated);
    }

    private static (List<PushedSubGraph> Pushed, List<PushdownLeftInMemory> InMemory) Report(
        IGraphPipelineConfiguration graph, List<(PipelineComponentConfiguration Component, IPushdownPart Part, PushdownAttempt Attempt)> tops, ILogger logger)
    {
        var pushed = new List<PushedSubGraph>();
        var inMemory = new List<PushdownLeftInMemory>();
        foreach (var (component, part, attempt) in tops)
        {
            if (attempt.Side is { } side && attempt.BatchSize >= 1)
            {
                var subGraph = PushedSubGraph.Of(component, part.OutputName, attempt, side, attempt.BatchSize);
                PushdownLog.SubGraphPushed(logger, graph.Name, component.Name, string.Join(", ", side.Members), side.Primary.DataStore, subGraph.Shape);
                pushed.Add(subGraph);
                continue;
            }

            var reason = attempt.Side is null ? attempt.Reason : "it states no batch size for the rows of a pushed query";
            PushdownLog.LeftInMemory(logger, graph.Name, component.Name, reason);
            inMemory.Add(new PushdownLeftInMemory(component.Name, reason));
        }

        return (pushed, inMemory);
    }
}
