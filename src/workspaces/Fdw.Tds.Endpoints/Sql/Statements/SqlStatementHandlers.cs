using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements;

/// <summary>
/// The T-SQL statements this endpoint runs, one handler per parsed statement type.
/// </summary>
/// <remarks>
/// A statement no handler runs is refused: a write or definition as SQL Server refuses a write to a
/// read-only database, error 3906; anything else as error 40517 naming the statement. The set is open — a
/// package adds a statement by declaring a handler.
/// </remarks>
[TypeCollection(typeof(SqlStatementHandlerBase), typeof(ISqlStatementHandler), typeof(SqlStatementHandlers))]
[ExcludeFromCodeCoverage]
public abstract partial class SqlStatementHandlers : TypeCollectionBase<SqlStatementHandlerBase, ISqlStatementHandler>
{
    private static readonly Lazy<Dictionary<Type, ISqlStatementHandler>> ByStatementType =
        new(static () => All().ToDictionary(h => h.StatementType, h => (ISqlStatementHandler)h));

    /// <summary>Finds the handler for a statement.</summary>
    /// <param name="statement">The statement as parsed.</param>
    /// <param name="handler">The handler, when one runs it.</param>
    /// <returns>True when a handler runs it.</returns>
    public static bool TryFor(TSqlStatement statement, [NotNullWhen(true)] out ISqlStatementHandler? handler)
    {
        ArgumentNullException.ThrowIfNull(statement);
        return ByStatementType.Value.TryGetValue(statement.GetType(), out handler);
    }

    /// <summary>Refuses a statement no handler runs, with the error SQL Server raises for it here.</summary>
    /// <param name="statement">The statement.</param>
    /// <param name="database">The current database.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The refusal.</returns>
    public static IGenericResult<SqlStatementOutcome> Refuse(TSqlStatement statement, string database, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(statement);
        SqlServerMessage message = IsWrite(statement)
            ? SqlServerErrors.ReadOnly(database, statement.StartLine)
            : SqlServerErrors.NotSupported(SqlText.Of(statement), statement.StartLine);
        return SqlRefusal.Refuse<SqlStatementOutcome>(message, logger);
    }

    // A data change, or a statement that creates, alters, drops, grants or truncates something.
    private static bool IsWrite(TSqlStatement statement)
    {
        if (statement is DataModificationStatement)
            return true;

        var kind = statement.GetType().Name;
        return kind.StartsWith("Create", StringComparison.Ordinal)
            || kind.StartsWith("Alter", StringComparison.Ordinal)
            || kind.StartsWith("Drop", StringComparison.Ordinal)
            || kind.StartsWith("Truncate", StringComparison.Ordinal)
            || kind.StartsWith("Grant", StringComparison.Ordinal)
            || kind.StartsWith("Revoke", StringComparison.Ordinal)
            || kind.StartsWith("Deny", StringComparison.Ordinal);
    }
}
