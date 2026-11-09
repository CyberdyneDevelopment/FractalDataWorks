using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sql;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL dialect implementation for Microsoft SQL Server (T-SQL).
/// Emits <c>[identifier]</c> quoting, <c>OFFSET/FETCH</c> paging, and <c>1 = 0</c> as the
/// always-false predicate.
/// </summary>
/// <remarks>
/// <see cref="TSqlDialect"/> is a stateless singleton — always use <see cref="Instance"/>.
/// </remarks>
public sealed class TSqlDialect : ISqlDialect, IKeyListSpillDialect
{
    /// <summary>
    /// Gets the singleton instance of <see cref="TSqlDialect"/>.
    /// </summary>
    public static readonly TSqlDialect Instance = new TSqlDialect();


    private TSqlDialect()
    {
    }

    /// <inheritdoc/>
    public string Name => "TSql";

    /// <inheritdoc/>
    public bool SupportsSchemaNamespace => true;

    /// <inheritdoc/>
    /// <remarks>
    /// T-SQL uses square-bracket quoting: <c>[identifier]</c>.
    /// </remarks>
    public string QuoteIdentifier(string identifier) => $"[{identifier}]";

    /// <inheritdoc/>
    public string ParameterPrefix => "@";

    /// <inheritdoc/>
    /// <remarks>
    /// A temporary table typed like the filtered column: <c>SELECT TOP 0</c> of the column through <c>ISNULL</c> takes its type and collation and leaves out an
    /// identity property, so the key table joins without a conversion and takes explicit values. It is filled from the one JSON parameter in the same batch,
    /// and dropped after the statement.
    /// </remarks>
    public KeyListSpillForm KeyListSpill(string uniqueId, string sourceTable, string columnName, string jsonParameter)
    {
        var table = $"#fdw_k_{uniqueId}";
        var column = QuoteIdentifier(columnName);
        return new KeyListSpillForm(
            $"SELECT TOP 0 ISNULL({column}, {column}) AS [Value] INTO {table} FROM {sourceTable}; "
            + $"INSERT INTO {table} ([Value]) SELECT [Value] FROM OPENJSON({jsonParameter}) WITH ([Value] nvarchar(max) '$'); ",
            $"SELECT [Value] FROM {table}",
            $" DROP TABLE {table}; ");
    }

    /// <inheritdoc/>
    public string KeyListJson(string columnName, IReadOnlyList<object?> values) => KeyListJsonWriter.Scalars(values);

    /// <inheritdoc/>
    public char LikeEscapeCharacter => '!';

    /// <inheritdoc/>
    /// <remarks>
    /// T-SQL reads <c>%</c> and <c>_</c> as wildcards and <c>[</c> as the start of a character set.
    /// </remarks>
    public string LikeMetaCharacters => "%_[";

    /// <inheritdoc/>
    public string PatternMatchOperator => "LIKE";

    /// <inheritdoc/>
    /// <remarks>
    /// T-SQL: <c>1 = 0</c>.
    /// </remarks>
    public string AlwaysFalsePredicate => "1 = 0";

    /// <inheritdoc/>
    /// <remarks>
    /// Emits SQL Server 2012+ syntax: <c>OFFSET n ROWS FETCH NEXT m ROWS ONLY</c>.
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

        return $"OFFSET {paging.Skip} ROWS FETCH NEXT {paging.Take} ROWS ONLY";
    }
}
