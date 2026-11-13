using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Lists the data sets one data set reads, whatever its kind (FDW-5290, FDW-5299): the one reading the save-time check and the read-time check of nested data sets share.
/// </summary>
/// <remarks>
/// A Variation's root, each source of a Compound or Federated data set and each data set a Graph data set's components, stage lookups and compute tables name are all the
/// same edge. What a Graph reads comes from <see cref="GraphDataSetReads"/>, the one reader of that, so a data set read by a stage tool is an edge as much as one a component
/// names. A source a Graph only intends to read feeds nothing yet and is not an edge.
/// </remarks>
internal static class GraphDataSetNestedReads
{
    /// <summary>Lists the data set names a data set reads, each once, without regard to case.</summary>
    /// <param name="dataSet">The data set, read deep enough to hold what it reads (a Graph with its stages).</param>
    /// <returns>The names, in the order the data set states them.</returns>
    internal static List<string> Of(IDataSetImplementationConfiguration dataSet) =>
    [
        .. new[] { dataSet.SourceDataSetName }
            .Concat(dataSet.Sources.Select(source => source.SourceDataSetName))
            .Concat(dataSet is IGraphDataSetConfiguration graph ? GraphReads(graph) : [])
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase),
    ];

    private static IEnumerable<string?> GraphReads(IGraphDataSetConfiguration graph) =>
        GraphDataSetReads.Of(graph)
            .Where(read => !read.IsIntended && string.Equals(read.Kind, GraphDataSetReadKinds.DataSet, StringComparison.Ordinal))
            .Select(read => read.Name);
}
