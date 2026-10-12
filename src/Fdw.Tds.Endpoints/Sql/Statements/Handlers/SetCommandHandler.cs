using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SET LANGUAGE, DATEFIRST, DATEFORMAT, LOCK_TIMEOUT, DEADLOCK_PRIORITY, QUERY_GOVERNOR_COST_LIMIT and
/// CONTEXT_INFO.
/// </summary>
/// <remarks>
/// DATEFIRST and LOCK_TIMEOUT are kept on the session. LANGUAGE must name the server's own language, since
/// no other one's messages exist here. The settings that govern locking, cost and context change nothing a
/// read returns here and are accepted. DATEFORMAT is accepted only as mdy, the order every date literal is
/// read in.
/// </remarks>
[TypeOption(typeof(SqlStatementHandlers), "SetCommand", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SetCommandHandler : SqlStatementHandlerBase<SetCommandStatement>
{
    private static readonly Dictionary<string, Func<SqlExecutionContext, string, bool>> Commands = new(StringComparer.Ordinal)
    {
        ["Language"] = static (c, v) => string.Equals(v, c.Server.Language, StringComparison.OrdinalIgnoreCase),
        ["DateFirst"] = static (c, v) => Whole(v, 1, 7, n => c.Session.DateFirst = n),
        ["LockTimeout"] = static (c, v) => Whole(v, -1, int.MaxValue, n => c.Session.LockTimeout = n),
        ["DateFormat"] = static (_, v) => string.Equals(v, "mdy", StringComparison.OrdinalIgnoreCase),
        ["DeadlockPriority"] = static (_, _) => true,
        ["QueryGovernorCostLimit"] = static (_, _) => true,
        ["ContextInfo"] = static (_, _) => true,
    };

    /// <summary>Initializes a new instance of the <see cref="SetCommandHandler"/> class.</summary>
    public SetCommandHandler()
        : base("SetCommand")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(SetCommandStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        foreach (var command in statement.Commands)
        {
            if (command is not GeneralSetCommand general)
                continue;

            var value = Value(general.Parameter, context);
            if (value.IsFailure)
                return Task.FromResult(value.ToNewResult<SqlStatementOutcome>());

            if (!Commands.TryGetValue(general.CommandType.ToString(), out var apply) || !apply(context, value.Value!))
                return Refuse(SqlServerErrors.NotSupported(SqlText.Of(general), general.StartLine), context);
        }

        return Ran();
    }

    // A bare word (SET LANGUAGE us_english) is read as written; anything else is evaluated.
    private static IGenericResult<string> Value(ScalarExpression parameter, SqlExecutionContext context)
    {
        if (parameter is Literal literal)
            return GenericResult<string>.Success(literal.Value);

        var value = SqlExpressions.Evaluate(parameter, SqlScope.Of(context));
        return value.IsFailure
            ? value.ToNewResult<string>()
            : GenericResult<string>.Success(value.Value is null ? string.Empty : SqlValues.ToText(value.Value));
    }

    private static bool Whole(string text, int min, int max, Action<int> keep)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number < min || number > max)
            return false;

        keep(number);
        return true;
    }
}
