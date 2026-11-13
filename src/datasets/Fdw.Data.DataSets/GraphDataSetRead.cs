using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.DataSets;

/// <summary>One thing a Graph data set reads, or intends to read (FDW-5296): a data set, a data store, or a container in one.</summary>
/// <param name="Kind">What it is: one of <see cref="GraphDataSetReadKinds"/>.</param>
/// <param name="Name">Its name.</param>
/// <param name="Parents">The names it sits under, outermost first: the store and path of a container; empty for the rest.</param>
/// <param name="IsIntended">True when it is an intended source (story 9) and not something the graph reads.</param>
public sealed record GraphDataSetRead(string Kind, string Name, IReadOnlyList<string> Parents, bool IsIntended)
{
    /// <summary>Gets the logical id of the data set an intent cites; null for a read the graph holds by name and for what is not a data set.</summary>
    public Guid? DataSetId { get; init; }

    /// <summary>Gets the name of the data store the read is from: a data store's own name, the store a container is under; null for a data set.</summary>
    public string? DataStoreName =>
        string.Equals(Kind, GraphDataSetReadKinds.DataStore, StringComparison.Ordinal) ? Name : Parents.Count > 0 ? Parents[0] : null;

    /// <summary>Whether this names the same thing as another: the same kind, name and parents, compared without regard to case.</summary>
    /// <param name="other">The other read.</param>
    /// <returns><see langword="true"/> when both name the same thing, whether or not either is intended.</returns>
    public bool Names(GraphDataSetRead other) =>
        string.Equals(Kind, other.Kind, StringComparison.Ordinal)
        && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
        && Parents.Count == other.Parents.Count
        && Parents.Zip(other.Parents, (a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase)).All(same => same);
}
