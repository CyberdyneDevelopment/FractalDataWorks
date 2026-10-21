using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql;

/// <summary>
/// The quoted names SQL-family translators write into their statements, by the path's own dialect. Both the data command
/// translators and the vectorized ones read them here.
/// </summary>
public static class SqlNames
{
    /// <summary>
    /// Builds the dialect-correct fully-qualified table name from a database path.
    /// Includes the database segment when present; omits the schema segment when the dialect
    /// does not support a schema namespace (e.g., SQLite).
    /// Examples:
    /// <list type="bullet">
    /// <item>TSql, database present: <c>[Database].[Schema].[Object]</c></item>
    /// <item>TSql, no database: <c>[Schema].[Object]</c></item>
    /// <item>SQLite (schemaless): <c>"Object"</c></item>
    /// </list>
    /// </summary>
    /// <param name="path">The database path providing the three-part name and dialect.</param>
    /// <returns>The quoted name.</returns>
    public static string QualifiedTable(IDatabasePath path)
    {
        var dialect = path.Dialect;
        var quotedObject = dialect.QuoteIdentifier(path.ObjectName);

        if (!dialect.SupportsSchemaNamespace || string.IsNullOrEmpty(path.Schema))
            return quotedObject;

        var schemaQualified = $"{dialect.QuoteIdentifier(path.Schema)}.{quotedObject}";

        return string.IsNullOrEmpty(path.Database)
            ? schemaQualified
            : $"{dialect.QuoteIdentifier(path.Database)}.{schemaQualified}";
    }
}
