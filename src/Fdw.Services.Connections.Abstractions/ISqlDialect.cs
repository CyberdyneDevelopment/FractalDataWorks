using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Defines the SQL dialect deltas for a specific database engine.
/// </summary>
/// <remarks>
/// <para>
/// Each SQL-family connection backend supplies an <see cref="ISqlDialect"/> singleton on its
/// path class. The shared <c>SqlDataCommandTranslatorBase</c> reads the dialect from the path
/// at translate-time and emits engine-specific SQL through these members — identifier quoting,
/// paging syntax, and the always-false predicate for empty-IN subqueries.
/// </para>
/// <para>
/// Qualified table names (schema.table, database.schema.table) are composed by the translator
/// from <see cref="QuoteIdentifier"/> and <see cref="SupportsSchemaNamespace"/>; no separate
/// method is needed on the dialect itself.
/// </para>
/// </remarks>
public interface ISqlDialect
{
    /// <summary>
    /// Gets the dialect name (e.g., "TSql", "PlPgSql", "Sqlite").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether this dialect supports a schema namespace between the database name and the
    /// object name (e.g., SQL Server's <c>dbo</c>, PostgreSQL's <c>public</c>). SQLite has no
    /// schema namespace; <see cref="IDatabasePath.Schema"/> is null for schemaless dialects.
    /// </summary>
    bool SupportsSchemaNamespace { get; }

    /// <summary>
    /// Returns the dialect-specific quoted form of a single identifier part.
    /// For T-SQL: <c>[identifier]</c>; for PostgreSQL/SQLite: <c>"identifier"</c>.
    /// </summary>
    /// <param name="identifier">The bare identifier to quote (must not contain the quote characters).</param>
    string QuoteIdentifier(string identifier);

    /// <summary>
    /// Gets the parameter prefix for this dialect (e.g., <c>@</c> for T-SQL and PostgreSQL,
    /// <c>:</c> for Oracle).
    /// </summary>
    string ParameterPrefix { get; }

    /// <summary>
    /// Gets a SQL predicate that always evaluates to false, used as the body of empty-IN
    /// subqueries (e.g., <c>col IN (SELECT NULL WHERE 1 = 0)</c>).
    /// T-SQL: <c>1 = 0</c>; PostgreSQL/SQLite: <c>FALSE</c>.
    /// </summary>
    string AlwaysFalsePredicate { get; }

    /// <summary>
    /// Builds the dialect-specific paging clause from a paging expression.
    /// T-SQL: <c>OFFSET n ROWS FETCH NEXT m ROWS ONLY</c>;
    /// PostgreSQL/SQLite: <c>LIMIT m OFFSET n</c>.
    /// </summary>
    /// <param name="paging">The paging expression with Skip and Take values.</param>
    /// <returns>The complete paging clause SQL fragment.</returns>
    string BuildPagingClause(IPagingExpression paging);
}
