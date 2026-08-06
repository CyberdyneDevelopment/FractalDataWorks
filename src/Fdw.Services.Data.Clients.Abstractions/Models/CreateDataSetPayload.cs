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
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = "1.0";
    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = "Standard";
    /// <summary>Gets or sets the service option type.</summary>
    public string ServiceOptionType { get; set; } = "Standard";
    /// <summary>Gets or sets the record type name.</summary>
    public string? RecordTypeName { get; set; }
    /// <summary>Gets or sets the fields.</summary>
    public IList<CreateDataSetFieldRequest> Fields { get; set; } = [];
    /// <summary>Gets or sets the sources.</summary>
    public IList<CreateDataSetSourcePayload> Sources { get; set; } = [];
    /// <summary>Gets or sets the joins.</summary>
    public IList<DataSetJoinPayload> Joins { get; set; } = [];
    /// <summary>Gets or sets the caching configuration.</summary>
    public DataSetCachingPayload Caching { get; set; } = new();
    /// <summary>Gets or sets the stored filter conditions applied when querying this DataSet.</summary>
    public IList<DataSetFilterConditionPayload> Filters { get; set; } = [];
}
