using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Lineage information for a DataSet.
/// </summary>
public sealed class DataSetLineagePayload
{
    /// <summary>Gets or sets the target DataSet name.</summary>
    public string DataSetName { get; set; } = string.Empty;
    /// <summary>Gets or sets upstream sources.</summary>
    public IReadOnlyList<LineageSourcePayload> UpstreamSources { get; set; } = Array.Empty<LineageSourcePayload>();
    /// <summary>Gets or sets downstream consumers.</summary>
    public IReadOnlyList<LineageConsumerPayload> DownstreamConsumers { get; set; } = Array.Empty<LineageConsumerPayload>();
    /// <summary>Gets or sets field-level lineage.</summary>
    public IReadOnlyList<FieldLineagePayload> FieldLineage { get; set; } = Array.Empty<FieldLineagePayload>();
}
