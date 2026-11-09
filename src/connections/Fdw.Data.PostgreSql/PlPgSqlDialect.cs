using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// SQL dialect implementation for PostgreSQL (PL/pgSQL).
/// Emits <c>"identifier"</c> quoting, <c>LIMIT/OFFSET</c> paging, and <c>FALSE</c> as the
/// always-false predicate.
/// </summary>
/// <remarks>
/// <see cref="PlPgSqlDialect"/> is a stateless singleton — always use <see cref="Instance"/>.
/// </remarks>
public sealed class PlPgSqlDialect : ISqlDialect, IKeyListSpillDialect
{
    /// <summary>
    /// Gets the singleton instance of <see cref="PlPgSqlDialect"/>.
    /// </summary>
    public static readonly PlPgSqlDialect Instance = new PlPgSqlDialect();

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
    /// One JSON parameter and no table to make or drop: <c>json_populate_recordset</c> reads the list typed by the filtered table's own row type, so each value
    /// takes its column's type. This is the form of a temporary table that needs no round trip and no cleanup.
    /// </remarks>
    public KeyListSpillForm KeyListSpill(string uniqueId, string sourceTable, string columnName, string jsonParameter)
        => new(
            string.Empty,
            $"SELECT k.{QuoteIdentifier(columnName)} FROM json_populate_recordset(null::{sourceTable}, {jsonParameter}::json) AS k",
            string.Empty);

    /// <inheritdoc/>
    public string KeyListJson(string columnName, IReadOnlyList<object?> values) => KeyListJsonWriter.Objects(columnName, values);

    /// <inheritdoc/>
    public char LikeEscapeCharacter => '!';

    /// <inheritdoc/>
    /// <remarks>
    /// PostgreSQL reads <c>%</c> and <c>_</c> as wildcards.
    /// </remarks>
    public string LikeMetaCharacters => "%_";

    /// <inheritdoc/>
    public string PatternMatchOperator => "ILIKE";

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
