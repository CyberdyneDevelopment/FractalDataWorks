using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// DTO representing a field within a data set.
/// </summary>
public class DataSetFieldResponse
{
    /// <summary>Gets or sets the field identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the fully qualified type name of the field.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the field's data type.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field is nullable.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets whether the field is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets whether the field is a key field.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets the maximum length for string fields.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the field's ordinal position.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the field description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the field role (e.g., key, measure, dimension).</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets whether this field participates as a join key for cross-source joins.</summary>
    public bool IsJoinKey { get; set; }

    /// <summary>Gets or sets the name of the configured calculation that computes this field's value when <see cref="IsCalculated"/> is true.</summary>
    public string? CalculationName { get; set; }

    /// <summary>Gets or sets whether this field is calculated via a configured calculation.</summary>
    public bool IsCalculated { get; set; }
}
