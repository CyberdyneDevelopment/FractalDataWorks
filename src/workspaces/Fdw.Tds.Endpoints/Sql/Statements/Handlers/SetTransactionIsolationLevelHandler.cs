using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SET TRANSACTION ISOLATION LEVEL: kept on the session. Every read here sees its store's committed rows,
/// so no level changes what a query returns.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "SetTransactionIsolationLevel", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetTransactionIsolationLevelHandler : SqlStatementHandlerBase<SetTransactionIsolationLevelStatement>
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal)
    {
        ["ReadCommitted"] = "READ COMMITTED",
        ["ReadUncommitted"] = "READ UNCOMMITTED",
        ["RepeatableRead"] = "REPEATABLE READ",
        ["Serializable"] = "SERIALIZABLE",
        ["Snapshot"] = "SNAPSHOT",
    };

    /// <summary>Initializes a new instance of the <see cref="SetTransactionIsolationLevelHandler"/> class.</summary>
    public SetTransactionIsolationLevelHandler()
        : base("SetTransactionIsolationLevel")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(SetTransactionIsolationLevelStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        if (!Names.TryGetValue(statement.Level.ToString(), out var level))
            return Refuse(SqlServerErrors.NotSupported(SqlText.Of(statement), statement.StartLine), context);

        context.Session.IsolationLevel = level;
        return Ran();
    }
}
