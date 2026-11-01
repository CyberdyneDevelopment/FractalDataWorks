using System;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// A schema as the system catalog lists it: its name and its <c>schema_id</c>.
/// </summary>
public sealed class CatalogSchema
{
    /// <summary>Initializes a new instance of the <see cref="CatalogSchema"/> class.</summary>
    /// <param name="name">The schema's name.</param>
    /// <param name="schemaId">Its id.</param>
    /// <param name="isSystem">Whether it is one of SQL Server's own schemas rather than one the caller reads.</param>
    public CatalogSchema(string name, int schemaId, bool isSystem)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SchemaId = schemaId;
        IsSystem = isSystem;
    }

    /// <summary>Gets the schema's name.</summary>
    public string Name { get; }

    /// <summary>Gets its id.</summary>
    public int SchemaId { get; }

    /// <summary>Gets a value indicating whether it is one of SQL Server's own schemas.</summary>
    public bool IsSystem { get; }
}
