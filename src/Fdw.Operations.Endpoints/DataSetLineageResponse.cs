using System;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// Lineage information for a DataSet.
/// </summary>
public class DataSetLineageResponse
{
    /// <summary>Gets or sets the DataSet name.</summary>
    public string DataSetName { get; set; } = string.Empty;
    /// <summary>Gets or sets the upstream data sources.</summary>
    public IList<LineageSourceResponse> UpstreamSources { get; set; } = [];
    /// <summary>Gets or sets the downstream consumers.</summary>
    public IList<LineageConsumerResponse> DownstreamConsumers { get; set; } = [];
    /// <summary>Gets or sets field-level lineage mappings.</summary>
    public IList<FieldLineageResponse> FieldLineage { get; set; } = [];
}