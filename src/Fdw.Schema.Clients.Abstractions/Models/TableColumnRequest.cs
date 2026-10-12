namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Represents a column in a table creation request.
/// </summary>
public sealed class TableColumnRequest
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the data type.</summary>
    public string DataType { get; set; } = "String";
    /// <summary>Gets or sets the maximum length.</summary>
    public int? MaxLength { get; set; }
    /// <summary>Gets or sets a value indicating whether the column is required.</summary>
    public bool IsRequired { get; set; }
    /// <summary>Gets or sets the default value.</summary>
    public string? DefaultValue { get; set; }
}
