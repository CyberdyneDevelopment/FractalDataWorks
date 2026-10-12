using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Functions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// USE database: switches to master or to the database this server serves, as SQL Server reports a switch.
/// </summary>
/// <remarks>Any other database is error 911, the error SQL Server raises for a database it does not hold.</remarks>
[TypeOption(typeof(SqlStatementHandlers), "Use", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UseHandler : SqlStatementHandlerBase<UseStatement>
{
    /// <summary>Initializes a new instance of the <see cref="UseHandler"/> class.</summary>
    public UseHandler()
        : base("Use")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(UseStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var requested = statement.DatabaseName.Value;
        if (Databases.IdOf(requested, context) is not { } id)
        {
            return Refuse(new SqlServerMessage(911, 1, 16,
                $"Database '{requested}' does not exist. Make sure that the name is entered correctly.", statement.StartLine), context);
        }

        var name = Databases.NameOf(id, context)!;
        var previous = context.Session.Database;
        context.Session.Database = name;
        var payload = context.Response.Payload;
        TdsTokenWriter.EnvironmentChange(payload, TdsEnvironmentChangeTypes.Database, name, previous);
        TdsTokenWriter.Message(payload, new SqlServerMessage(5701, 2, 0, $"Changed database context to '{name}'.", statement.StartLine), context.Server.ServerName);
        return Ran();
    }
}
