using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a column schema for UI display.
/// </summary>
public sealed class ColumnSchemaPayload : IColumnSchema
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the data type.</summary>
    public string DataType { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the column allows nulls.</summary>
    public bool IsNullable { get; set; }
    /// <summary>Gets or sets the maximum length.</summary>
    public int? MaxLength { get; set; }
    /// <summary>Gets or sets the numeric precision.</summary>
    public int? Precision { get; set; }
    /// <summary>Gets or sets the numeric scale.</summary>
    public int? Scale { get; set; }
    /// <summary>Gets or sets the inferred property role.</summary>
    public string? Role { get; set; }
}
