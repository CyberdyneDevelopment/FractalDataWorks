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
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = "1.0";
    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = "Standard";
    /// <summary>Gets or sets the service option type.</summary>
    public string ServiceOptionType { get; set; } = "Standard";
    /// <summary>Gets or sets the record type name.</summary>
    public string? RecordTypeName { get; set; }
    /// <summary>Gets or sets the updated fields.</summary>
    public IReadOnlyList<CreateDataSetFieldRequest> Fields { get; set; } = Array.Empty<CreateDataSetFieldRequest>();
    /// <summary>Gets or sets the updated sources.</summary>
    public IReadOnlyList<CreateDataSetSourcePayload> Sources { get; set; } = Array.Empty<CreateDataSetSourcePayload>();
    /// <summary>Gets or sets the updated joins.</summary>
    public IReadOnlyList<DataSetJoinPayload> Joins { get; set; } = Array.Empty<DataSetJoinPayload>();
    /// <summary>Gets or sets the updated caching configuration.</summary>
    public DataSetCachingPayload Caching { get; set; } = new();
    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IReadOnlyList<DataSetFilterConditionPayload> Filters { get; set; } = Array.Empty<DataSetFilterConditionPayload>();
}
