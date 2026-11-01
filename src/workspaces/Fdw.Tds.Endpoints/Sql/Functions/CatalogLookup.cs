using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Catalog;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Functions;

/// <summary>
/// The catalog functions — OBJECT_ID, OBJECT_NAME, SCHEMA_NAME and their kin — answered from the catalog
/// the request read.
/// </summary>
/// <remarks>
/// The batch runner reads the catalog before a statement that calls one of these, so the lookups here never
/// wait on the endpoint. A name the caller may not read answers NULL, exactly as one that does not exist.
/// </remarks>
public static class CatalogLookup
{
    /// <summary>The user_type_id of sysname, which is nvarchar(128) under its own name.</summary>
    public const int SysnameTypeId = 256;

    /// <summary>The function names that read the catalog, so the batch runner reads it first.</summary>
    public static readonly IReadOnlySet<string> Functions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "OBJECT_ID", "OBJECT_NAME", "OBJECT_SCHEMA_NAME", "SCHEMA_NAME", "SCHEMA_ID",
    };

    /// <summary>SCHEMA_NAME(id).</summary>
    /// <param name="call">The call.</param>
    /// <param name="id">The schema id; null for NULL.</param>
    /// <returns>The schema's name, or NULL.</returns>
    public static IGenericResult<object?> SchemaName(SqlFunctionCall call, long? id)
    {
        var catalog = Loaded(call);
        if (catalog.IsFailure)
            return catalog.ToNewResult<object?>();

        return SqlFunctionCall.Ok(catalog.Value!.Schemas.FirstOrDefault(s => s.SchemaId == id)?.Name);
    }

    /// <summary>SCHEMA_ID(name).</summary>
    /// <param name="call">The call.</param>
    /// <param name="name">The schema's name; null for NULL.</param>
    /// <returns>The schema's id, or NULL.</returns>
    public static IGenericResult<object?> SchemaId(SqlFunctionCall call, string? name)
    {
        var catalog = Loaded(call);
        if (catalog.IsFailure)
            return catalog.ToNewResult<object?>();

        return SqlFunctionCall.Ok(name is not null && catalog.Value!.TryFindSchema(name, out var schema) ? schema.SchemaId : null);
    }

    /// <summary>OBJECT_ID(name[, type]).</summary>
    /// <param name="call">The call.</param>
    /// <param name="name">The object's name, one to three parts; null for NULL.</param>
    /// <param name="type">The object type asked for, or null for any.</param>
    /// <returns>The table's id, or NULL.</returns>
    public static IGenericResult<object?> ObjectId(SqlFunctionCall call, string? name, string? type)
    {
        var catalog = Loaded(call);
        if (catalog.IsFailure)
            return catalog.ToNewResult<object?>();

        // Every object here is a user table; asking for any other type finds nothing.
        if (name is null || (type is not null && !string.Equals(type.Trim(), "U", StringComparison.OrdinalIgnoreCase)))
            return SqlFunctionCall.Ok(null);

        return SqlFunctionCall.Ok(Parts(name) is { } parts
            && Databases.IsKnown(parts.DatabaseIdentifier?.Value, call.Context)
            && parts.SchemaIdentifier is { } schema
            && catalog.Value!.TryFind(schema.Value, parts.BaseIdentifier.Value, out var table)
                ? table.ObjectId
                : null);
    }

    /// <summary>OBJECT_NAME(id) and OBJECT_SCHEMA_NAME(id).</summary>
    /// <param name="call">The call.</param>
    /// <param name="id">The object id; null for NULL.</param>
    /// <param name="schema">Whether the schema's name is asked for rather than the object's.</param>
    /// <returns>The name, or NULL.</returns>
    public static IGenericResult<object?> ObjectName(SqlFunctionCall call, long? id, bool schema)
    {
        var catalog = Loaded(call);
        if (catalog.IsFailure)
            return catalog.ToNewResult<object?>();

        if (id is not { } objectId || objectId is < int.MinValue or > int.MaxValue || !catalog.Value!.TryFind((int)objectId, out var table))
            return SqlFunctionCall.Ok(null);

        return SqlFunctionCall.Ok(schema ? table.Table.Schema : table.Table.Name);
    }

    /// <summary>TYPE_NAME(id).</summary>
    /// <param name="id">The user_type_id; null for NULL.</param>
    /// <returns>The type's name, or null.</returns>
    public static string? TypeName(long? id)
    {
        if (id == SysnameTypeId)
            return "sysname";

        return SqlDataTypes.All().FirstOrDefault(t => t.SystemTypeId == id)?.SqlName;
    }

    private static IGenericResult<CatalogSnapshot> Loaded(SqlFunctionCall call)
        => call.Context.LoadedCatalog is { } catalog
            ? GenericResult<CatalogSnapshot>.Success(catalog)
            : SqlRefusal.Refuse<CatalogSnapshot>(
                new SqlServerMessage(50000, 1, 16, $"{call.Name} needs the catalog, which was not read for this statement.", call.Line),
                call.Context.Logger);

    // A one- to three-part name as T-SQL parses it, brackets and quotes included.
    private static SchemaObjectName? Parts(string name)
    {
        using var reader = new StringReader(name);
        var parsed = new TSql160Parser(true).ParseSchemaObjectName(reader, out var errors);
        return errors.Count == 0 ? parsed : null;
    }
}
