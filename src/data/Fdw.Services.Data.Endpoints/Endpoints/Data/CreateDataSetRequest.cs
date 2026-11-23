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

    /// <summary>Gets or sets the display name a person reads the data set by; left out or empty, the data set has none (FDW-6117).</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the abbreviation a person may read the data set by, at most 20 characters; left out or empty, the data set has none (FDW-6129).</summary>
    public string? Abbreviation { get; set; }

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

    /// <summary>Gets or sets the data set version. Left out, the kind's declared version applies (FDW-3956).</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the key field names.</summary>
    public IList<string> KeyFields { get; set; } = [];

    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IList<DataSetFilterConditionPayload> Filters { get; set; } = [];

    /// <summary>
    /// Gets or sets the federation strategy used to combine sources for a Federated dataset (e.g.
    /// "Sequential", "Parallel", "Optimized" — a registered <c>FederationStrategies</c> member).
    /// Allowed only when the sources make the data set Federated (FDW-6806: the kind is worked out from the sources, never stated).
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

    /// <summary>
    /// Gets or sets how a Compound or Federated data set's sources combine: "Join" (by its joins) or "Union" (their rows stacked; every source
    /// maps every field), a <c>DataSetCompositionTypes</c> member that takes several sources (FDW-6946). Required when the data set has more than one source; nothing is assumed.
    /// </summary>
    public string? SourceCombination { get; set; }

    /// <summary>
    /// Gets or sets a Federated data set's own values of its strategy's options (FDW-6901), which win over the FDW-level settings: ReadsInParallel,
    /// MaxParallelReads, OrdersSources. A name the strategy does not have, or a value that does not parse, is refused.
    /// </summary>
    public IList<DataSetFederationOptionPayload> FederationOptions { get; set; } = [];

    /// <summary>Gets or sets the aggregate measure definitions for this data set.</summary>
    public IList<CreateDataSetAggregateRequest> Aggregates { get; set; } = [];

    /// <summary>Gets or sets the caching configuration.</summary>
    public DataSetCachingPayload? Caching { get; set; }
}
