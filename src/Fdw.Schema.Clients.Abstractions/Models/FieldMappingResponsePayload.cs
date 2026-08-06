using System;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Response Payload for a persisted field mapping.
/// </summary>
public sealed class FieldMappingResponsePayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the DataSet source identifier.</summary>
    public Guid DataSetSourceId { get; set; }
    /// <summary>Gets or sets the source name.</summary>
    public string SourceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the logical field name.</summary>
    public string LogicalFieldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical field name.</summary>
    public string PhysicalFieldName { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional transform expression.</summary>
    public string? TransformExpression { get; set; }
}
