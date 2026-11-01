using System;
using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// What a data set name names: the data set whose fields and rows it reads, and what kind of thing the name is (FDW-2390).
/// </summary>
/// <param name="DataSet">The data set whose fields and rows are read: the data set itself, or the root data set a variation varies.</param>
/// <param name="Variation">The variation the name is, with its own name, id, display name and description; null when the name is a data set.</param>
/// <param name="ParentName">The name of the thing it varies directly: the parent view, else the root data set; null for a data set.</param>
/// <param name="ViewFilters">The stored filter of every view in the chain that has one, the topmost parent first and the view itself last; empty for a data set (FDW-2391).</param>
public sealed record ResolvedDataSet(
    IDataSetImplementationConfiguration DataSet,
    IDataSetVariationImplementationConfiguration? Variation,
    string? ParentName,
    IReadOnlyList<string> ViewFilters)
{
    /// <summary>The kind of a name that is a data set and not a variation of one.</summary>
    public const string DataSetKind = "DataSet";

    /// <summary>Gets what the name is: <see cref="DataSetKind"/>, else the kind of variation (the name of its implementation, <c>DataView</c> for a saved view).</summary>
    public string Kind => Variation?.Implementation ?? DataSetKind;

    /// <summary>Gets the name that was resolved.</summary>
    public string Name => Variation?.Name ?? DataSet.Name;

    /// <summary>Gets the logical id of the thing the name names.</summary>
    public Guid Id => Variation?.Id ?? DataSet.Id;
}
