using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to create a new DataSet.
/// </summary>
public sealed class CreateDataSetPayload
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the display name a person reads the data set by; left out or empty, the data set has none.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the abbreviation a person may read the data set by, at most 20 characters; left out or empty, the data set has none.</summary>
    public string? Abbreviation { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public string? Version { get; set; }
    /// <summary>Gets or sets the name of the DataSet category the data set sits under; left out, it sits under none.</summary>
    public string? Category { get; set; }
    /// <summary>Gets or sets the record type name.</summary>
    public string? RecordTypeName { get; set; }
    /// <summary>Gets or sets the fields.</summary>
    public IList<CreateDataSetFieldRequest> Fields { get; set; } = [];
    /// <summary>Gets or sets the sources.</summary>
    public IList<CreateDataSetSourcePayload> Sources { get; set; } = [];
    /// <summary>Gets or sets the joins.</summary>
    public IList<DataSetJoinPayload> Joins { get; set; } = [];
    /// <summary>Gets or sets how the data set's sources combine, "Join" or "Union" (FDW-6946).</summary>
    public string? SourceCombination { get; set; }
    /// <summary>Gets or sets a Federated data set's own federation option values (FDW-6901).</summary>
    public IList<DataSetFederationOptionPayload> FederationOptions { get; set; } = [];
    /// <summary>Gets or sets the caching configuration.</summary>
    public DataSetCachingPayload Caching { get; set; } = new();
    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IList<DataSetFilterConditionPayload> Filters { get; set; } = [];
}
