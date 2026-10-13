using System;
using Fdw.Data;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Internal entity for querying pipeline configurations for lineage tracking.
/// </summary>
[GenerateMapper]
public partial class PipelineLineageRecord
{
    /// <summary>Gets or sets the pipeline identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the pipeline name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the pipeline type (maps to Implementation column).</summary>
    public string Implementation { get; set; } = string.Empty;
    /// <summary>Gets or sets the source DataSet name.</summary>
    public string? SourceDataSet { get; set; }
    /// <summary>Gets or sets the destination DataSet name.</summary>
    public string? DestinationDataSet { get; set; }
    /// <summary>Gets or sets whether the pipeline is enabled.</summary>
    public bool IsEnabled { get; set; }
    /// <summary>Gets or sets the data store the pipeline reads, when its source is a data store container (maps to SourceDataStoreName).</summary>
    public string? SourceDataStoreName { get; set; }
    /// <summary>Gets or sets the data store the pipeline writes, when its destination is a data store container (maps to DestinationDataStoreName).</summary>
    public string? DestinationDataStoreName { get; set; }
    /// <summary>Gets or sets the source container within the data store (maps to SourceContainerName).</summary>
    public string? SourceContainerName { get; set; }
    /// <summary>Gets or sets the destination container within the data store (maps to DestinationContainerName).</summary>
    public string? DestinationContainerName { get; set; }
}