using System;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Statistics about the dataflow graph.
/// </summary>
public class DataflowStatsDto
{
    /// <summary>Gets or sets the number of DataSet nodes.</summary>
    public int DataSetCount { get; set; }
    /// <summary>Gets or sets the number of DataStore nodes.</summary>
    public int DataStoreCount { get; set; }
    /// <summary>Gets or sets the number of Connection nodes.</summary>
    public int ConnectionCount { get; set; }
    /// <summary>Gets or sets the number of Source nodes.</summary>
    public int SourceCount { get; set; }
    /// <summary>Gets or sets the total number of edges.</summary>
    public int EdgeCount { get; set; }
}