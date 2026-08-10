using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Detailed information for a DataSet.
/// </summary>
public sealed class DataSetDetailPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the name (stable identifier).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the human-facing display name. Falls back to Name when null.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the short abbreviation for compact UI contexts.</summary>
    public string? Abbreviation { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = "1.0";
    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = "Standard";
    /// <summary>Gets or sets the service option type.</summary>
    public string ServiceOptionType { get; set; } = "Standard";
    /// <summary>Gets or sets the record type name.</summary>
    public string? RecordTypeName { get; set; }
    /// <summary>Gets or sets the fields.</summary>
    public IReadOnlyList<DataSetFieldPayload> Fields { get; set; } = Array.Empty<DataSetFieldPayload>();
    /// <summary>Gets or sets the key field names.</summary>
    public IReadOnlyList<string> KeyFields { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets the surrogate key field names.</summary>
    public IReadOnlyList<string> SurrogateKeyFields { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets the natural key field names.</summary>
    public IReadOnlyList<string> NaturalKeyFields { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets the sources.</summary>
    public IReadOnlyList<DataSetSourcePayload> Sources { get; set; } = Array.Empty<DataSetSourcePayload>();
    /// <summary>Gets or sets the joins.</summary>
    public IReadOnlyList<DataSetJoinPayload> Joins { get; set; } = Array.Empty<DataSetJoinPayload>();
    /// <summary>Gets or sets the caching configuration.</summary>
    public DataSetCachingPayload Caching { get; set; } = new();
    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IReadOnlyList<DataSetFilterConditionPayload> Filters { get; set; } = Array.Empty<DataSetFilterConditionPayload>();
    /// <summary>Gets or sets the creation timestamp.</summary>
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
