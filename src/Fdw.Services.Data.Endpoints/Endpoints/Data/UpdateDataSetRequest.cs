using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for updating an existing data set.
/// </summary>
public class UpdateDataSetRequest
{
    /// <summary>Gets or sets the data set name (identifier).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data set description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data set category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the data set version.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the key field names.</summary>
    public IList<string>? KeyFields { get; set; }

    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IList<DataSetFilterConditionPayload>? Filters { get; set; }

    /// <summary>
    /// Gets or sets the dataset discriminator. Bound from the wizard's <c>serviceOptionType</c> field.
    /// </summary>
    [JsonPropertyName("serviceOptionType")]
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets or sets the federation strategy used to combine sources for a Federated dataset (e.g.
    /// "Sequential", "Parallel", "Optimized" — a registered <c>FederationStrategies</c> member).
    /// Required when <see cref="ServiceOptionType"/> is "Federated"; must be null otherwise.
    /// </summary>
    public string? FederationStrategy { get; set; }

    /// <summary>Gets or sets the SQL or expression that transforms the source data for a Compound dataset.</summary>
    public string? TransformExpression { get; set; }

    /// <summary>Gets or sets the name of the source dataset this Compound dataset is derived from.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets the composed fields for this data set.</summary>
    public IList<CreateDataSetFieldRequest>? Fields { get; set; }

    /// <summary>Gets or sets the composed sources for this data set.</summary>
    public IList<CreateDataSetSourceRequest>? Sources { get; set; }

    /// <summary>Gets or sets the joins between sources.</summary>
    public IList<DataSetJoinPayload>? Joins { get; set; }

    /// <summary>Gets or sets the aggregate measure definitions for this data set.</summary>
    public IList<CreateDataSetAggregateRequest>? Aggregates { get; set; }

    /// <summary>Gets or sets the caching configuration.</summary>
    public DataSetCachingPayload? Caching { get; set; }
}
