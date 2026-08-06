using System;

namespace Fdw.Data.Components.Models;

/// <summary>
/// A mapping between a source and target field.
/// </summary>
public sealed class FieldMappingDto
{
    /// <summary>Gets or sets the mapping ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the source field name.</summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the target field name.</summary>
    public string TargetField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mapping type name.
    /// Well-known values are defined in <see cref="MappingTypes"/>.
    /// </summary>
    public string MappingType { get; set; } = MappingTypes.Direct.Name;

    /// <summary>Gets or sets whether this mapping was auto-generated.</summary>
    public bool IsAutoMapped { get; set; }

    /// <summary>Gets or sets whether this mapping overrides an auto-mapped value.</summary>
    public bool IsOverridden { get; set; }

    /// <summary>Gets or sets the transform expression (for Transform mappings).</summary>
    public string? TransformExpression { get; set; }

    /// <summary>Gets or sets the constant value (for Constant mappings).</summary>
    public string? ConstantValue { get; set; }
}
