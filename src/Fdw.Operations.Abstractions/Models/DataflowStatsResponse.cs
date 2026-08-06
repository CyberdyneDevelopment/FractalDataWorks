namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Statistics for a dataflow graph.
/// </summary>
// Why: pure response DTO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class DataflowStatsResponse
{
    /// <summary>Gets or sets the number of DataSets.</summary>
    public int DataSetCount { get; set; }
    /// <summary>Gets or sets the number of DataStores.</summary>
    public int DataStoreCount { get; set; }
    /// <summary>Gets or sets the number of data sources.</summary>
    public int SourceCount { get; set; }
    /// <summary>Gets or sets the number of connections.</summary>
    public int ConnectionCount { get; set; }
}
