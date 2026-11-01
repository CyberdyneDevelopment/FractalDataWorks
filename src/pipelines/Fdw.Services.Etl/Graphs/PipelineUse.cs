using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Etl.Graphs;

/// <summary>One thing a pipeline uses and what it does with it (FDW-1683): the data set it reads, the container it writes, the pipeline it starts.</summary>
/// <param name="Relation">What the pipeline does with it: one of <see cref="PipelineUseRelations"/>.</param>
/// <param name="Kind">What it is, named as the catalog names its kinds: DataSet, DataStore, Container or Pipeline.</param>
/// <param name="Name">Its name.</param>
/// <param name="Parents">The names it sits under, outermost first: the store and path of a container; empty for the rest.</param>
public sealed record PipelineUse(string Relation, string Kind, string Name, IReadOnlyList<string> Parents)
{
    /// <summary>Gets the path a destination of the File kind writes at in place of the declared container's (FDW-3193); none for every other use.</summary>
    public string? PhysicalPath { get; init; }

    /// <summary>Gets the file a destination of the File kind writes in place of the declared container's (FDW-3193); none for every other use.</summary>
    public string? PhysicalName { get; init; }

    /// <summary>Whether this is the same use as another: the same relation on the same thing, compared without regard to case.</summary>
    /// <param name="other">The other use.</param>
    /// <returns><see langword="true"/> when both name the same relation, kind, name and parents.</returns>
    public bool Matches(PipelineUse other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return string.Equals(Relation, other.Relation, StringComparison.Ordinal)
            && string.Equals(Kind, other.Kind, StringComparison.Ordinal)
            && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(PhysicalPath ?? string.Empty, other.PhysicalPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            && string.Equals(PhysicalName ?? string.Empty, other.PhysicalName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            && Parents.Count == other.Parents.Count
            && Parents.Zip(other.Parents, (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase)).All(same => same);
    }
}
