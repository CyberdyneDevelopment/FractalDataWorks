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
    /// <summary>Gets or sets the pipeline type (maps to ServiceOptionType column).</summary>
    public string ServiceOptionType { get; set; } = string.Empty;
    /// <summary>Gets or sets the source DataSet name.</summary>
    public string? SourceDataSet { get; set; }
    /// <summary>Gets or sets the destination DataSet name.</summary>
    public string? DestinationDataSet { get; set; }
    /// <summary>Gets or sets whether the pipeline is enabled.</summary>
    public bool IsEnabled { get; set; }
    /// <summary>Gets or sets the source connection name.</summary>
    public string? SourceConnectionName { get; set; }
    /// <summary>Gets or sets the destination connection name (maps to DestinationConnectionName column).</summary>
    public string? DestinationConnectionName { get; set; }
}