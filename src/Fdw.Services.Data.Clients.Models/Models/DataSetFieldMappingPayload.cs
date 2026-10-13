using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a field mapping between DataSet and source.
/// </summary>
public sealed class DataSetFieldMappingPayload
{
    /// <summary>Gets or sets the durable logical identifier for this field mapping (FK to data.DataSetFieldMapping.Id).</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the DataSet field name.</summary>
    public string DataSetFieldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the source name.</summary>
    public string SourceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the source field name.</summary>
    /// <remarks>Null when the source has no physical field to name (a DataSet or Calculation source).</remarks>
    public string? SourceFieldName { get; set; }
    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }
}
