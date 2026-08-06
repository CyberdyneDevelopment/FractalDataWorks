using System;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// SQL dialect implementation for PostgreSQL (PL/pgSQL).
/// Emits <c>"identifier"</c> quoting, <c>LIMIT/OFFSET</c> paging, and <c>FALSE</c> as the
/// always-false predicate.
/// </summary>
/// <remarks>
/// <see cref="PlPgSqlDialect"/> is a stateless singleton — always use <see cref="Instance"/>.
/// </remarks>
public sealed class PlPgSqlDialect : ISqlDialect
{
    /// <summary>
    /// Gets the singleton instance of <see cref="PlPgSqlDialect"/>.
    /// </summary>
    public static readonly PlPgSqlDialect Instance = new PlPgSqlDialect();

    // Why: private constructor enforces singleton usage — the dialect carries no state,
    // so creating multiple instances would be pointless allocation.
    private PlPgSqlDialect()
    {
    }

    /// <inheritdoc/>
    public string Name => "PlPgSql";

    /// <inheritdoc/>
    public bool SupportsSchemaNamespace => true;

    /// <inheritdoc/>
    /// <remarks>
    /// PostgreSQL uses double-quote quoting: <c>"identifier"</c>.
    /// </remarks>
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    /// <inheritdoc/>
    public string ParameterPrefix => "@";

    /// <inheritdoc/>
    /// <remarks>
    /// PostgreSQL: <c>FALSE</c>.
    /// </remarks>
    public string AlwaysFalsePredicate => "FALSE";

    /// <inheritdoc/>
    /// <remarks>
    /// Emits PostgreSQL native syntax: <c>LIMIT n OFFSET m</c>.
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
