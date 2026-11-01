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
    /// <summary>
    /// Gets or sets the graph pipelines that read the DataSet by name and whose fields could not be followed to a destination (FDW-2157): the describe
    /// failed, or no field of the DataSet reaches a destination with its origin. They may use fields the consumers do not list. A pipeline whose
    /// DataSet is chosen by a run parameter is not found by name and is in neither this list nor the consumers.
    /// </summary>
    public IList<string> PipelinesNotFollowed { get; set; } = [];
}