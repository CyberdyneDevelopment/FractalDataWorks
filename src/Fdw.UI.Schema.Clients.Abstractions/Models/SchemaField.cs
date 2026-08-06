namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Represents a column or field within a schema entity.
/// </summary>
public sealed class SchemaField
{
    /// <summary>
    /// Gets or sets the column name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SQL data type of the column.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the column accepts null values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this column is a foreign key.
    /// </summary>
    public bool IsForeignKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this column is an identity (auto-increment) column.
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this column is a computed column.
    /// </summary>
    public bool IsComputed { get; set; }

    /// <summary>
    /// Gets or sets the default value expression for this column, if any.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the column.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the one-based ordinal position of the column within the entity.
    /// </summary>
    public int OrdinalPosition { get; set; }

    /// <summary>
    /// Gets or sets the maximum length (in characters or bytes) for string or binary columns.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the numeric precision for decimal or numeric columns.
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Gets or sets the numeric scale (digits after the decimal point) for decimal or numeric columns.
    /// </summary>
    public int? Scale { get; set; }
}
