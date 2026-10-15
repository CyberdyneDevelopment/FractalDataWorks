using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// The data stores a pipeline reads from and writes to, for the lineage graphs (FDW-1269). A pipeline names a data store container
/// as its source or destination; the graph shows the store as one node, the way users meet connections.
/// </summary>
internal static class PipelineLineageStores
{
    /// <summary>Gets the distinct names of the data stores the pipelines read or write.</summary>
    /// <param name="pipelines">The pipelines.</param>
    /// <returns>The store names, compared ignoring case.</returns>
    public static IReadOnlyCollection<string> Names(IEnumerable<PipelineLineageRecord> pipelines)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pipeline in pipelines)
        {
            if (!string.IsNullOrEmpty(pipeline.SourceDataStoreName))
                names.Add(pipeline.SourceDataStoreName);
            if (!string.IsNullOrEmpty(pipeline.DestinationDataStoreName))
                names.Add(pipeline.DestinationDataStoreName);
        }

        return names;
    }

    /// <summary>Gets the id of the node that stands for a data store.</summary>
    /// <param name="name">The store's name.</param>
    /// <returns>The node id.</returns>
    public static string NodeId(string name) => $"DataStore_{name}";
}
