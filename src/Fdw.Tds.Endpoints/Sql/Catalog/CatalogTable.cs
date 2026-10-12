using System;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// A table as the system catalog lists it: the table, and the ids <c>sys.objects</c> and
/// <c>sys.schemas</c> give it.
/// </summary>
public sealed class CatalogTable
{
    /// <summary>Initializes a new instance of the <see cref="CatalogTable"/> class.</summary>
    /// <param name="table">The table.</param>
    /// <param name="objectId">Its object id.</param>
    /// <param name="schemaId">Its schema's id.</param>
    public CatalogTable(SqlTable table, int objectId, int schemaId)
    {
        Table = table ?? throw new ArgumentNullException(nameof(table));
        ObjectId = objectId;
        SchemaId = schemaId;
    }

    /// <summary>Gets the table.</summary>
    public SqlTable Table { get; }

    /// <summary>Gets its object id.</summary>
    public int ObjectId { get; }

    /// <summary>Gets its schema's id.</summary>
    public int SchemaId { get; }
}
