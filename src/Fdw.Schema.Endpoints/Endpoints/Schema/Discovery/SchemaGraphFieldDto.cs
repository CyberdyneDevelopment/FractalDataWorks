namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// A field within a schema graph entity.
/// </summary>
public class SchemaGraphFieldDto
{
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field is nullable.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets whether the field is a foreign key.</summary>
    public bool IsForeignKey { get; set; }

    /// <summary>Gets or sets whether the field is an identity column.</summary>
    public bool IsIdentity { get; set; }

    /// <summary>Gets or sets the ordinal position.</summary>
    public int OrdinalPosition { get; set; }
}
