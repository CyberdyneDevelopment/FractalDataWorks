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
    /// <summary>Gets or sets the physical field name, when the mapping names one.</summary>
    /// <remarks>
    /// Nullable because the column is: a mapping can name a logical field without binding it to a
    /// physical one yet. Reporting that as an empty string would say the mapping points at a field
    /// with no name, which is a different thing from pointing at nothing.
    /// </remarks>
    public string? PhysicalFieldName { get; set; }
    /// <summary>Gets or sets the optional transform expression.</summary>
    public string? TransformExpression { get; set; }
}
