using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to update a DataSet.
/// </summary>
public sealed class UpdateDataSetPayload
{
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the display name; left out, the stored one stays, and an empty string clears it.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the abbreviation; left out, the stored one stays, and an empty string clears it.</summary>
    public string? Abbreviation { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public string? Version { get; set; }
    /// <summary>Gets or sets the name of the DataSet category the data set sits under; left out, the current one stays, and an empty string takes the data set out of its category.</summary>
    public string? Category { get; set; }
    /// <summary>Gets or sets the record type name.</summary>
    public string? RecordTypeName { get; set; }
    /// <summary>Gets or sets the updated fields.</summary>
    public IReadOnlyList<CreateDataSetFieldRequest>? Fields { get; set; }
    /// <summary>Gets or sets the updated sources.</summary>
    public IReadOnlyList<CreateDataSetSourcePayload>? Sources { get; set; }
    /// <summary>Gets or sets the updated joins.</summary>
    public IReadOnlyList<DataSetJoinPayload>? Joins { get; set; }
    /// <summary>Gets or sets how the data set's sources combine, "Join" or "Union" (FDW-6946).</summary>
    public string? SourceCombination { get; set; }
    /// <summary>Gets or sets a Federated data set's own federation option values (FDW-6901).</summary>
    public IList<DataSetFederationOptionPayload>? FederationOptions { get; set; }
    /// <summary>Gets or sets the updated caching configuration.</summary>
    public DataSetCachingPayload Caching { get; set; } = new();
    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IReadOnlyList<DataSetFilterConditionPayload>? Filters { get; set; }
}
