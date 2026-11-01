using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// What the system catalog shows one caller for the length of one request: SQL Server's own schemas, the
/// schemas the caller reads, and their tables.
/// </summary>
/// <remarks>
/// Read once per request on first use, from the endpoint, as the caller. A catalog view and a table lookup
/// in the same batch therefore see the same tables, and a caller sees only what it may read.
/// </remarks>
public sealed class CatalogSnapshot
{
    private CatalogSnapshot(IReadOnlyList<CatalogSchema> schemas, IReadOnlyList<CatalogTable> tables)
    {
        Schemas = schemas;
        Tables = tables;
    }

    /// <summary>Gets every schema, SQL Server's own first.</summary>
    public IReadOnlyList<CatalogSchema> Schemas { get; }

    /// <summary>Gets every table the caller may read.</summary>
    public IReadOnlyList<CatalogTable> Tables { get; }

    /// <summary>Reads the snapshot from the endpoint, as the caller.</summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The snapshot, or the failure reading it.</returns>
    public static async Task<IGenericResult<CatalogSnapshot>> Load(TdsEndpointBase endpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var names = await endpoint.Schemas(cancellationToken).ConfigureAwait(false);
        if (names.IsFailure)
            return names.ToNewResult<CatalogSnapshot>();

        var schemas = new List<CatalogSchema>
        {
            new("dbo", CatalogIds.Dbo, isSystem: true),
            new("INFORMATION_SCHEMA", CatalogIds.InformationSchema, isSystem: true),
            new("sys", CatalogIds.Sys, isSystem: true),
        };
        var tables = new List<CatalogTable>();

        foreach (var name in names.Value!)
        {
            var schemaId = CatalogIds.SchemaId(name);
            schemas.Add(new CatalogSchema(name, schemaId, isSystem: false));

            var listed = await endpoint.Tables(name, cancellationToken).ConfigureAwait(false);
            if (listed.IsFailure)
                return listed.ToNewResult<CatalogSnapshot>();

            tables.AddRange(listed.Value!.Select(t => new CatalogTable(t, CatalogIds.ObjectId(t.Schema, t.Name), schemaId)));
        }

        return GenericResult<CatalogSnapshot>.Success(new CatalogSnapshot(schemas, tables));
    }

    /// <summary>Finds a table by schema and name, ignoring case.</summary>
    /// <param name="schema">The schema.</param>
    /// <param name="name">The table's name.</param>
    /// <param name="table">The table, when the caller may read one by that name.</param>
    /// <returns>True when found.</returns>
    public bool TryFind(string schema, string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out CatalogTable? table)
    {
        table = Tables.FirstOrDefault(t =>
            string.Equals(t.Table.Schema, schema, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Table.Name, name, StringComparison.OrdinalIgnoreCase));
        return table is not null;
    }

    /// <summary>Finds a table by object id.</summary>
    /// <param name="objectId">The id.</param>
    /// <param name="table">The table, when one has that id.</param>
    /// <returns>True when found.</returns>
    public bool TryFind(int objectId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out CatalogTable? table)
    {
        table = Tables.FirstOrDefault(t => t.ObjectId == objectId);
        return table is not null;
    }

    /// <summary>Finds a schema by name, ignoring case.</summary>
    /// <param name="name">The schema's name.</param>
    /// <param name="schema">The schema, when there is one.</param>
    /// <returns>True when found.</returns>
    public bool TryFindSchema(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out CatalogSchema? schema)
    {
        schema = Schemas.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        return schema is not null;
    }
}
