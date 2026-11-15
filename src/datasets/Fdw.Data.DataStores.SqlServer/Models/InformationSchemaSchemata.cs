using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Data.DataStores.SqlServer.Models;

/// <summary>
/// A row of INFORMATION_SCHEMA.SCHEMATA (FDW-5876): the catalog (database) name every schema row carries.
/// </summary>
[GenerateMapper]
public sealed class InformationSchemaSchemata
{
    /// <summary>
    /// Gets or sets the name of the catalog (database) the schema is in.
    /// </summary>
    [Column("CATALOG_NAME")]
    public string CatalogName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name.
    /// </summary>
    [Column("SCHEMA_NAME")]
    public string SchemaName { get; set; } = string.Empty;
}
