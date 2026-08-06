using System;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL dialect implementation for Microsoft SQL Server (T-SQL).
/// Emits <c>[identifier]</c> quoting, <c>OFFSET/FETCH</c> paging, and <c>1 = 0</c> as the
/// always-false predicate.
/// </summary>
/// <remarks>
/// <see cref="TSqlDialect"/> is a stateless singleton — always use <see cref="Instance"/>.
/// </remarks>
public sealed class TSqlDialect : ISqlDialect
{
    /// <summary>
    /// Gets the singleton instance of <see cref="TSqlDialect"/>.
    /// </summary>
    public static readonly TSqlDialect Instance = new TSqlDialect();

    // Why: private constructor enforces singleton usage — the dialect carries no state,
    // so creating multiple instances would be pointless allocation.
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
