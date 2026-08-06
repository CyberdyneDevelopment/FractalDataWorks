using System.ComponentModel.DataAnnotations;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Input Payload for a field mapping in a save mappings request.
/// </summary>
public sealed class FieldMappingInputPayload
{
    /// <summary>Gets or sets the logical field name (DataSet/target field).</summary>
    [Required]
    public string LogicalFieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the physical field name (source field).</summary>
    [Required]
    public string PhysicalFieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the transform expression (optional).</summary>
    public string? TransformExpression { get; set; }
}
