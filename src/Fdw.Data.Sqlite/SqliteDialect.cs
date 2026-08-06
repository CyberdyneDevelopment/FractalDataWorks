using System;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.Sqlite;

/// <summary>
/// SQL dialect implementation for SQLite.
/// Emits <c>"identifier"</c> (double-quote) quoting, <c>LIMIT/OFFSET</c> paging, and <c>FALSE</c> as the
/// always-false predicate. SQLite has no schema namespace.
/// </summary>
/// <remarks>
/// <see cref="SqliteDialect"/> is a stateless singleton — always use <see cref="Instance"/>.
/// </remarks>
public sealed class SqliteDialect : ISqlDialect
{
    /// <summary>
    /// Gets the singleton instance of <see cref="SqliteDialect"/>.
    /// </summary>
    public static readonly SqliteDialect Instance = new SqliteDialect();

    // Why: private constructor enforces singleton usage — the dialect carries no state.
    private SqliteDialect()
    {
    }

    /// <inheritdoc/>
    public string Name => "Sqlite";

    /// <inheritdoc/>
    /// <remarks>
    /// SQLite has no schema namespace — all objects live in the attached database file.
    /// </remarks>
    public bool SupportsSchemaNamespace => false;

    /// <inheritdoc/>
    /// <remarks>
    /// SQLite uses double-quote quoting: <c>"identifier"</c>.
    /// </remarks>
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    /// <inheritdoc/>
    public string ParameterPrefix => "@";

    /// <inheritdoc/>
    /// <remarks>
    /// SQLite: <c>FALSE</c> (SQLite 3.23.0+, 2018).
    /// </remarks>
    public string AlwaysFalsePredicate => "FALSE";

    /// <inheritdoc/>
    /// <remarks>
    /// Emits SQLite syntax: <c>LIMIT m OFFSET n</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="IPagingExpression.Skip"/> is negative or
    /// <see cref="IPagingExpression.Take"/> is zero or negative.
    /// </exception>
    public string BuildPagingClause(IPagingExpression paging)
    {
        if (paging.Skip < 0)
        {
            throw new ArgumentException(
                $"Paging Skip must be >= 0, but was {paging.Skip}.",
                nameof(paging));
        }

        if (paging.Take <= 0)
        {
            throw new ArgumentException(
                $"Paging Take must be > 0, but was {paging.Take}.",
                nameof(paging));
        }

        return $"LIMIT {paging.Take} OFFSET {paging.Skip}";
    }
}
