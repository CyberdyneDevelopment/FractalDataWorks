using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Names the data sets a pipeline reads and writes (FDW-1683).</summary>
/// <remarks>
/// A graph pipeline holds them in the settings of its sources and destinations, so it must be read whole. One reading serves the
/// map, the lineage and the organization scope.
/// </remarks>
internal static class PipelineDataSets
{
    private const string DataSet = "DataSet";

    /// <summary>Lists the data sets the pipeline reads.</summary>
    /// <param name="pipeline">The pipeline; a graph pipeline read whole.</param>
    /// <returns>The names, each once.</returns>
    public static IReadOnlyList<string> Reads(IPipelineImplementationConfiguration pipeline) => Named(pipeline, PipelineUseRelations.Reads);

    /// <summary>Lists the data sets the pipeline writes.</summary>
    /// <param name="pipeline">The pipeline; a graph pipeline read whole.</param>
    /// <returns>The names, each once.</returns>
    public static IReadOnlyList<string> Writes(IPipelineImplementationConfiguration pipeline) => Named(pipeline, PipelineUseRelations.Writes);

    private static List<string> Named(IPipelineImplementationConfiguration pipeline, string relation) => pipeline switch
    {
        GraphPipelineConfiguration graph => GraphPipelineUses.Of(graph)
            .Where(use => string.Equals(use.Kind, DataSet, StringComparison.Ordinal) && string.Equals(use.Relation, relation, StringComparison.Ordinal))
            .Select(use => use.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList(),
        _ => [],
    };
}
