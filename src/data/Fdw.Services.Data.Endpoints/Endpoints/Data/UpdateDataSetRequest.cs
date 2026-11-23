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

    /// <summary>Gets or sets the display name; left out, the stored one stays, and an empty string clears it (FDW-6117).</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the abbreviation; left out, the stored one stays, and an empty string clears it (FDW-6129).</summary>
    public string? Abbreviation { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the data set's data classification level (see
    /// <c>GET /data-classifications</c>). An id that is not a level is refused. Left out, the current classification stays.
    /// </summary>
    public int? ClassificationId { get; set; }

    /// <summary>
    /// Gets or sets the name of the <c>DataSet</c> category the data set sits under. Left out, the current category stays; an empty
    /// string takes the data set out of its category. A name that is not a category of that type is refused.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the data set version. Left out, the stored version is kept.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the record type name.</summary>
    public string RecordTypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the key field names.</summary>
    public IList<string>? KeyFields { get; set; }

    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IList<DataSetFilterConditionPayload>? Filters { get; set; }

    /// <summary>
    /// Gets or sets the federation strategy used to combine sources for a Federated dataset (e.g.
    /// "Sequential", "Parallel", "Optimized" — a registered <c>FederationStrategies</c> member).
    /// Allowed only when the sources make the data set Federated (FDW-6806: the kind is worked out from the sources, never stated).
    /// </summary>
    public string? FederationStrategy { get; set; }

    /// <summary>Gets or sets the name of the source dataset this Compound dataset is derived from.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets the composed fields for this data set.</summary>
    public IList<CreateDataSetFieldRequest>? Fields { get; set; }

    /// <summary>Gets or sets the composed sources for this data set.</summary>
    public IList<CreateDataSetSourceRequest>? Sources { get; set; }

    /// <summary>Gets or sets the joins between sources.</summary>
    public IList<DataSetJoinPayload>? Joins { get; set; }

    /// <summary>
    /// Gets or sets how the data set's sources combine, "Join" or "Union" (FDW-6946); null keeps the stored statement.
    /// </summary>
    public string? SourceCombination { get; set; }

    /// <summary>
    /// Gets or sets a Federated data set's own values of its strategy's options (FDW-6901), which win over the FDW-level settings: ReadsInParallel,
    /// MaxParallelReads, OrdersSources. A name the strategy does not have, or a value that does not parse, is refused. Null keeps the stored values.
    /// </summary>
    public IList<DataSetFederationOptionPayload>? FederationOptions { get; set; }

    /// <summary>Gets or sets the aggregate measure definitions for this data set.</summary>
    public IList<CreateDataSetAggregateRequest>? Aggregates { get; set; }

    /// <summary>Gets or sets the caching configuration.</summary>
    public DataSetCachingPayload? Caching { get; set; }
}
