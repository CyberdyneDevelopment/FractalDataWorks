using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// What a saved pipeline reads and writes for one run, which the roles the run acts with must be able to reach (FDW-1087, FDW-1161,
/// FDW-1891): every data set and every data store container its components and stages use, each as a read or a write.
/// </summary>
/// <remarks>
/// <para>
/// Why this is read from <see cref="GraphPipelineUses"/> and nowhere else: a run is judged for reach when it starts, against the
/// pipeline as it is then stored, so starting it by hand, by a webhook call and by a schedule all answer from the one reading of what
/// the pipeline uses, and the lineage and the data map answer from the same one.
/// </para>
/// <para>
/// Why it takes the run's values: a source may name its data set by a parameter, and what the run reaches is the name it is given, so the
/// components are rendered with the values first, exactly as the run builds them.
/// </para>
/// <para>
/// A pipeline read as a record alone has no steps and so reaches nothing; the caller reads it whole.
/// </para>
/// </remarks>
/// <param name="PipelineName">The pipeline's name.</param>
/// <param name="DataSets">The data sets it reads or writes, each once.</param>
/// <param name="Containers">The data store containers it reads or writes, each once.</param>
public sealed record PipelineReach(
    string PipelineName,
    IReadOnlyList<ReachedDataSet> DataSets,
    IReadOnlyList<ReachedContainer> Containers)
{
    /// <summary>Reads what a pipeline reads and writes for a run given these values.</summary>
    /// <param name="graph">The pipeline, read whole.</param>
    /// <param name="parameters">The run's typed values by parameter name.</param>
    /// <param name="logger">The logger the failures are written to.</param>
    /// <returns>The pipeline's reach, or the failure that names the parameter the run was not given.</returns>
    public static IGenericResult<PipelineReach> Of(GraphPipelineConfiguration graph, IReadOnlyDictionary<string, object?> parameters, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var found = GraphPipelineUses.Of(graph, parameters, logger);
        if (found.IsFailure || found.Value is null)
        {
            return found.ToNewResult<PipelineReach>();
        }

        var uses = found.Value;
        return GenericResult<PipelineReach>.Success(new PipelineReach(
            graph.Name,
            [.. uses.Where(use => string.Equals(use.Kind, PipelineUseKinds.DataSet, StringComparison.Ordinal))
                .Select(use => new ReachedDataSet(use.Name, Writes(use)))
                .Distinct()],
            [.. uses.Where(use => string.Equals(use.Kind, PipelineUseKinds.Container, StringComparison.Ordinal))
                .Select(use => new ReachedContainer(use.Parents[0], use.Parents.Count > 1 ? use.Parents[1] : string.Empty, use.Name, Writes(use)))
                .Distinct()]));
    }

    private static bool Writes(PipelineUse use) => string.Equals(use.Relation, PipelineUseRelations.Writes, StringComparison.Ordinal);
}
