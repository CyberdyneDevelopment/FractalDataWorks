namespace Fdw.Data.DataStores.SqlServer.Catalog;

/// <summary>A table, view or procedure the catalog names (FDW-5876).</summary>
public sealed class CatalogObject
{
    /// <summary>Gets or sets the schema name.</summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>Gets or sets the object name.</summary>
    public string ObjectName { get; set; } = string.Empty;
}
