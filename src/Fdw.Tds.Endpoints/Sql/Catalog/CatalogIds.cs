using System;
using System.Text;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// The ids the system catalog gives objects that have none of their own.
/// </summary>
/// <remarks>
/// A client keeps an object id across requests — SSMS lists tables by id and expands one by it — so an id
/// must not change while the object does not. Each is derived from the object's name, not from where it
/// falls in a list, and lands above the range SQL Server keeps for its own objects.
/// </remarks>
public static class CatalogIds
{
    /// <summary>The id of the schema user tables would sit in on a real server; listed, holding nothing.</summary>
    public const int Dbo = 1;

    /// <summary>The id of the INFORMATION_SCHEMA schema.</summary>
    public const int InformationSchema = 3;

    /// <summary>The id of the sys schema.</summary>
    public const int Sys = 4;

    private const uint FnvOffset = 2166136261;
    private const uint FnvPrime = 16777619;
    private const int FirstUserId = 1 << 16;

    /// <summary>Gets the id of a schema the caller reads.</summary>
    /// <param name="schema">The schema's name.</param>
    /// <returns>Its id.</returns>
    public static int SchemaId(string schema) => Derive("schema:" + schema);

    /// <summary>Gets the id of a table.</summary>
    /// <param name="schema">The table's schema.</param>
    /// <param name="table">The table's name.</param>
    /// <returns>Its id.</returns>
    public static int ObjectId(string schema, string table) => Derive("table:" + schema + "." + table);

    // FNV-1a over the lower-cased name, kept positive and above SQL Server's own ids.
    private static int Derive(string name)
    {
        var hash = FnvOffset;
        foreach (var b in Encoding.UTF8.GetBytes(name.ToLowerInvariant()))
            hash = unchecked((hash ^ b) * FnvPrime);

        return (int)(hash & 0x7FFFFFFF) | FirstUserId;
    }
}
