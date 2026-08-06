using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a field mapping between DataSet and source.
/// </summary>
public sealed class DataSetFieldMappingPayload
{
    /// <summary>Gets or sets the durable logical identifier for this field mapping (FK to data.DataSetFieldMapping.Id).</summary>
    // Why: TransformChainEditor's [EditorRequired] FieldMappingId requires the persisted logical Id from
    // ConfigurationDb so the UI can load/manage the transform chain for a specific mapping row.
    public Guid Id { get; set; }
    /// <summary>Gets or sets the DataSet field name.</summary>
    public string DataSetFieldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the source name.</summary>
    public string SourceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the source field name.</summary>
    public string SourceFieldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the transform expression.</summary>
    public string? TransformExpression { get; set; }
    /// <summary>Gets or sets the mapping expression.</summary>
    public string? Expression { get => TransformExpression; set => TransformExpression = value; }
    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }
}
