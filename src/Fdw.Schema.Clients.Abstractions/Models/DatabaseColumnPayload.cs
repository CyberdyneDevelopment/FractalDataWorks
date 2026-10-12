namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Column from database discovery.
/// </summary>
public sealed class DatabaseColumnPayload
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the SQL data type.</summary>
    public string SqlType { get; set; } = string.Empty;
    /// <summary>Gets or sets the inferred .NET data type name.</summary>
    public string? DotNetType { get; set; }
    /// <summary>Gets or sets a value indicating whether the column allows nulls.</summary>
    public bool IsNullable { get; set; }
    /// <summary>Gets or sets a value indicating whether this is an identity column.</summary>
    public bool IsIdentity { get; set; }
    /// <summary>Gets or sets a value indicating whether this is a computed column.</summary>
    public bool IsComputed { get; set; }
    /// <summary>Gets or sets the maximum length.</summary>
    public int? MaxLength { get; set; }
    /// <summary>Gets or sets the numeric precision.</summary>
    public int? Precision { get; set; }
    /// <summary>Gets or sets the numeric scale.</summary>
    public int? Scale { get; set; }
    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }
    /// <summary>Gets or sets the inferred property role.</summary>
    public string? Role { get; set; }
}
