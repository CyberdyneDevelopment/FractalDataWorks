using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// The one test for whether a node's pipeline prerequisites can be run, shared by the save-time validator and the
/// run-time scheduler so that what is accepted when saved is exactly what can be run.
/// </summary>
internal static class PipelinePrerequisiteGraph
{
    /// <summary>Finds the pipelines that take part in a cycle, a pipeline that needs itself included; a pipeline that only depends on a cycle, or is needed by one, is not one.</summary>
    /// <param name="pipelineIds">The pipelines the node holds.</param>
    /// <param name="prerequisites">What each pipeline needs; a prerequisite with an end outside the node is ignored here and refused by the caller.</param>
    /// <returns>The cyclic pipelines, in the order of <paramref name="pipelineIds"/>; empty when the prerequisites can be run.</returns>
    internal static IReadOnlyList<Guid> FindCyclic(
        IEnumerable<Guid> pipelineIds,
        IEnumerable<OrchestrationNodePipelinePrerequisiteConfiguration> prerequisites)
    {
        var ids = pipelineIds.Distinct().ToList();
        var needs = ids.ToDictionary(id => id, _ => new List<Guid>());
        foreach (var prerequisite in prerequisites)
        {
            if (needs.TryGetValue(prerequisite.PipelineId, out var list) && needs.ContainsKey(prerequisite.PrerequisitePipelineId))
            {
                list.Add(prerequisite.PrerequisitePipelineId);
            }
        }

        // Kahn's algorithm from both ends: peel off every pipeline whose prerequisites are all gone, then every pipeline that no remaining
        // pipeline needs, until neither is left. What remains is on a cycle (or between cycles), never merely upstream or downstream of one.
        var remaining = ids.ToHashSet();
        bool peeled;
        do
        {
            var ready = remaining
                .Where(id => needs[id].All(need => !remaining.Contains(need)) ||
                             !remaining.Any(other => needs[other].Contains(id)))
                .ToList();
            peeled = ready.Count > 0;
            remaining.ExceptWith(ready);
        }
        while (peeled);

        return ids.Where(remaining.Contains).ToList();
    }
}
