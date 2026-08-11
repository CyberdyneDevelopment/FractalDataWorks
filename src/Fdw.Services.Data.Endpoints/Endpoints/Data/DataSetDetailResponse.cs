using System;
using System.Collections.Generic;
using Fdw.Web.Endpoints.Contracts;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Detailed DTO for a data set, including fields, keys, and sources.
/// </summary>
public class DataSetDetailResponse : ResourceDetail
{
    /// <summary>Gets or sets the human-facing display name. Falls back to Name when null.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the short abbreviation for compact UI contexts.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the data set description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data set version.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the data set category.</summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the dataset strategy discriminator: "Simple", "Compound", or "Federated"
    /// (a registered <c>DataSetTypes</c> member).
    /// </summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets or sets the federation strategy used to combine sources for a Federated dataset (a
    /// registered <c>FederationStrategies</c> member). Null for Standard and Compound datasets.
    /// </summary>
    public string? FederationStrategy { get; set; }

    /// <summary>Gets or sets the SQL or expression that transforms the source data for a Compound dataset.</summary>
    public string? TransformExpression { get; set; }

    /// <summary>Gets or sets the name of the source dataset this Compound dataset is derived from.</summary>
    public string? SourceDataSetName { get; set; }

    /// <summary>Gets or sets the fully qualified record type name.</summary>
    public string? RecordTypeName { get; set; }

    /// <summary>Gets or sets the collection of field definitions.</summary>
    public IList<DataSetFieldPayload> Fields { get; set; } = [];

    /// <summary>Gets or sets the surrogate key field names.</summary>
    public IList<string> SurrogateKeyFields { get; set; } = [];

    /// <summary>Gets or sets the natural key field names.</summary>
    public IList<string> NaturalKeyFields { get; set; } = [];

    /// <summary>Gets or sets the collection of data sources.</summary>
    public IList<DataSetSourcePayload> Sources { get; set; } = [];

    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IList<DataSetFilterConditionPayload> Filters { get; set; } = [];

    /// <summary>Gets or sets the join definitions composing this DataSet's sources.</summary>
    // Why: the editor must round-trip joins. Without them on the detail, loading a DataSet for edit
    // and saving silently drops every join (the update path persists exactly what the form holds).
    public IList<DataSetJoinPayload> Joins { get; set; } = [];

    /// <summary>Gets or sets the caching policy for this DataSet, or <c>null</c> when not configured.</summary>
    // Why: same round-trip reason as Joins — omitting Caching from the detail makes an edit wipe it.
    public DataSetCachingPayload? Caching { get; set; }

    /// <summary>Gets or sets the aggregate measure definitions composed on this DataSet.</summary>
    // Why: same round-trip reason as Joins/Caching — omitting Aggregates from the detail makes an
    // edit silently drop every aggregate measure (WI-5).
    public IList<DataSetAggregateDto> Aggregates { get; set; } = [];

    /// <summary>Gets or sets the creation timestamp.</summary>
    // Why: DateTimeOffset serializes with timezone offset (ISO 8601 compliant) — plain DateTime
    // produces a string without offset which fails ISO regex assertions.
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>Gets or sets the user who created the record.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the user who last modified the record.</summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreatedOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifiedOnBehalfOf { get; set; } = string.Empty;
}
