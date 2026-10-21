using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// What the lineage knows of one pipeline: its identity and what it reads and writes (FDW-1885).
/// </summary>
/// <remarks>
/// A pipeline reads and writes any number of data sets and containers, so the record holds the pipeline's uses, as
/// <see cref="GraphPipelineUses"/> reads them from the pipeline read whole, and answers the data sets and containers each way
/// from them. A pipeline that was not read whole has no uses and is drawn as a node with no lines.
/// </remarks>
public sealed class PipelineLineageRecord
{
    /// <summary>Gets or sets the pipeline identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the pipeline name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the pipeline type (maps to Implementation column).</summary>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the pipeline is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets what the pipeline uses, in the order its steps hold them.</summary>
    public IReadOnlyList<PipelineUse> Uses { get; set; } = [];

    /// <summary>Gets the names of the data sets the pipeline reads, each once.</summary>
    public IEnumerable<string> DataSetsRead => DataSetsOf(PipelineUseRelations.Reads);

    /// <summary>Gets the names of the data sets the pipeline writes, each once.</summary>
    public IEnumerable<string> DataSetsWritten => DataSetsOf(PipelineUseRelations.Writes);

    /// <summary>Gets the containers the pipeline reads, each once.</summary>
    internal IEnumerable<PipelineLineageContainer> ContainersRead => ContainersOf(PipelineUseRelations.Reads);

    /// <summary>Gets the containers the pipeline writes, each once.</summary>
    internal IEnumerable<PipelineLineageContainer> ContainersWritten => ContainersOf(PipelineUseRelations.Writes);

    private IEnumerable<string> DataSetsOf(string relation) =>
        Uses.Where(use => IsOf(use, PipelineUseKinds.DataSet, relation))
            .Select(use => use.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    // A container's parents are its store and then its path (GraphPipelineUses); the store is the first.
    private IEnumerable<PipelineLineageContainer> ContainersOf(string relation) =>
        Uses.Where(use => IsOf(use, PipelineUseKinds.Container, relation) && use.Parents.Count > 0)
            .Select(use => new PipelineLineageContainer(use.Parents[0], use.Name))
            .Distinct();

    private static bool IsOf(PipelineUse use, string kind, string relation) =>
        string.Equals(use.Kind, kind, StringComparison.Ordinal) && string.Equals(use.Relation, relation, StringComparison.Ordinal);
}
