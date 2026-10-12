namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Mapping from logical field to physical source field.
/// </summary>
public sealed class FieldSourceMappingPayload
{
    /// <summary>Gets or sets the physical source name.</summary>
    public string SourceName { get; set; } = string.Empty;
    /// <summary>Gets or sets the physical field name.</summary>
    public string PhysicalField { get; set; } = string.Empty;
}
