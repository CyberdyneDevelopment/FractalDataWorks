namespace Fdw.Data.Components.Models;

/// <summary>
/// A field in a schema, used by the data mapper provider.
/// </summary>
public sealed class SchemaFieldDto
{
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type (e.g., "varchar", "int", "datetime2").</summary>
    public string? DataType { get; set; }

    /// <summary>Gets or sets whether the field is nullable.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets the maximum length for string types.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets the precision for numeric types.</summary>
    public int? Precision { get; set; }

    /// <summary>Gets or sets the scale for numeric types.</summary>
    public int? Scale { get; set; }
}
