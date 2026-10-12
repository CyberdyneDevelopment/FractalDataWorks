namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Shared interface for SQL database path objects, providing structured access to the
/// three-part name (Database, Schema, ObjectName) and the dialect that governs SQL emission.
/// </summary>
/// <remarks>
/// <para>
/// SQL-family path classes (e.g., <c>DatabasePath</c> for MsSql,
/// <c>PostgreSqlDatabasePath</c> for PostgreSql) implement this interface alongside their
/// existing type hierarchy. The shared translator base uses a pattern-match
/// <c>container.Path is IDatabasePath dbPath</c> to obtain the dialect and structured name
/// without referencing any backend-specific type.
/// </para>
/// <para>
/// <see cref="Schema"/> is nullable to support schemaless dialects (SQLite). Dialects where
/// a schema namespace is required (T-SQL, PlPgSql) fail-loud at path construction when Schema
/// is absent — they never produce a nullable Schema at runtime.
/// </para>
/// </remarks>
public interface IDatabasePath
{
    /// <summary>
    /// Gets the database name, or <c>null</c> / empty when the connection's context implies
    /// the database (single-database connections).
    /// </summary>
    string? Database { get; }

    /// <summary>
    /// Gets the schema name (e.g., <c>dbo</c>, <c>public</c>), or <c>null</c> for schemaless
    /// dialects such as SQLite.
    /// </summary>
    string? Schema { get; }

    /// <summary>
    /// Gets the object name (table, view, etc.).
    /// </summary>
    string ObjectName { get; }

    /// <summary>
    /// Gets the SQL dialect for this path, which determines identifier quoting, paging syntax,
    /// and other engine-specific SQL fragments.
    /// </summary>
    ISqlDialect Dialect { get; }
}
