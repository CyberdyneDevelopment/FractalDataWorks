using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// A row of sys.columns (FDW-5876): the identity and computed flags INFORMATION_SCHEMA does not carry.
/// </summary>
[GenerateMapper]
public sealed class SysColumn
{
    /// <summary>Gets or sets the owning object's id.</summary>
    [Column("object_id")]
    public int ObjectId { get; set; }

    /// <summary>Gets or sets the column name.</summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the column is an identity column.</summary>
    [Column("is_identity")]
    public bool IsIdentity { get; set; }

    /// <summary>Gets or sets whether the column is computed.</summary>
    [Column("is_computed")]
    public bool IsComputed { get; set; }
}
