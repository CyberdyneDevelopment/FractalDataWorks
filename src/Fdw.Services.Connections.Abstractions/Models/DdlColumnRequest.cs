namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// Column definition for table creation via DDL execution.
/// </summary>
public sealed class DdlColumnRequest
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the SQL data type name.</summary>
    public string SqlType { get; set; } = "NVARCHAR";

    /// <summary>Gets or sets the max length for string/binary types.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets whether the column is required (NOT NULL).</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets whether the column is an identity column.</summary>
    public bool IsIdentity { get; set; }
}
