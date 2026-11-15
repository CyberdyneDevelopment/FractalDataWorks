using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// A row of sys.objects (FDW-5876).
/// </summary>
[GenerateMapper]
public sealed class SysObject
{
    /// <summary>Gets or sets the object id.</summary>
    [Column("object_id")]
    public int ObjectId { get; set; }

    /// <summary>Gets or sets the object name.</summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning schema's id.</summary>
    [Column("schema_id")]
    public int SchemaId { get; set; }
}
