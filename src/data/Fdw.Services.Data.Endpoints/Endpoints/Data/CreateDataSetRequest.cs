using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for creating a new data set.
/// </summary>
public class CreateDataSetRequest
{
    /// <summary>Gets or sets the data set name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the data set's data classification level (see
    /// <c>GET /data-classifications</c>). Left out, it is <c>NotYetClassified</c>. An id that is not a level is refused.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>
    /// Gets or sets the name of the <c>DataSet</c> category the data set sits under (see <c>GET /categories?type=DataSet</c>). Left out,
    /// it sits under none. A name that is not a category of that type is refused.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the data set version.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the key field names.</summary>
    public IList<string> KeyFields { get; set; } = [];

    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IList<DataSetFilterConditionPayload> Filters { get; set; } = [];

    /// <summary>
    /// Gets or sets the dataset strategy discriminator: "Simple", "Compound", or "Federated"
    /// (a registered <c>DataSetTypes</c> member). Bound from the wizard's <c>implementation</c> field.
    /// </summary>
    [JsonPropertyName("implementation")]
    public string Implementation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the federation strategy used to combine sources for a Federated dataset (e.g.
    /// "Sequential", "Parallel", "Optimized" — a registered <c>FederationStrategies</c> member).
    /// Required when <see cref="Implementation"/> is "Federated"; must be null otherwise.
    /// </summary>
    public string? FederationStrategy { get; set; }

    /// <summary>Gets or sets the name of the source dataset this Compound dataset is derived from.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets the composed fields for this data set.</summary>
    public IList<CreateDataSetFieldRequest> Fields { get; set; } = [];

    /// <summary>Gets or sets the composed sources for this data set.</summary>
    public IList<CreateDataSetSourceRequest> Sources { get; set; } = [];

    /// <summary>Gets or sets the joins between sources.</summary>
    public IList<DataSetJoinPayload> Joins { get; set; } = [];

    /// <summary>Gets or sets the aggregate measure definitions for this data set.</summary>
    public IList<CreateDataSetAggregateRequest> Aggregates { get; set; } = [];

    /// <summary>Gets or sets the caching configuration.</summary>
    public DataSetCachingPayload? Caching { get; set; }
}
