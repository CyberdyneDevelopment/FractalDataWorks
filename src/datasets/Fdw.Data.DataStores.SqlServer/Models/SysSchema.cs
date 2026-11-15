using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// A row of sys.schemas (FDW-5876).
/// </summary>
[GenerateMapper]
public sealed class SysSchema
{
    /// <summary>Gets or sets the schema id.</summary>
    [Column("schema_id")]
    public int SchemaId { get; set; }

    /// <summary>Gets or sets the schema name.</summary>
    [Column("name")]
    public string Name { get; set; } = string.Empty;
}
