using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Names the containers a pipeline reads and writes directly, with no data set between (FDW-3824).</summary>
/// <remarks>The same reading as <see cref="PipelineDataSets"/>: a graph pipeline holds them in the settings of its sources and destinations, so it must be read whole.</remarks>
internal static class PipelineContainers
{
    private const string Container = "Container";

    /// <summary>Lists the containers the pipeline reads.</summary>
    /// <param name="pipeline">The pipeline; a graph pipeline read whole.</param>
    /// <returns>The containers, each once.</returns>
    public static IReadOnlyList<PipelineContainerRef> Reads(IPipelineImplementationConfiguration pipeline) => Named(pipeline, PipelineUseRelations.Reads);

    /// <summary>Lists the containers the pipeline writes.</summary>
    /// <param name="pipeline">The pipeline; a graph pipeline read whole.</param>
    /// <returns>The containers, each once.</returns>
    public static IReadOnlyList<PipelineContainerRef> Writes(IPipelineImplementationConfiguration pipeline) => Named(pipeline, PipelineUseRelations.Writes);

    // A use names its container, with the data store first and then the path in Parents; a use with no store names no container the map can find.
    private static List<PipelineContainerRef> Named(IPipelineImplementationConfiguration pipeline, string relation) => pipeline switch
    {
        GraphPipelineConfiguration graph => GraphPipelineUses.Of(graph)
            .Where(use => string.Equals(use.Kind, Container, StringComparison.Ordinal)
                && string.Equals(use.Relation, relation, StringComparison.Ordinal)
                && use.Parents.Count > 0)
            .Select(use => new PipelineContainerRef(use.Parents[0], use.Parents.Count > 1 ? use.Parents[1] : null, use.Name))
            .Distinct()
            .ToList(),
        _ => [],
    };
}
