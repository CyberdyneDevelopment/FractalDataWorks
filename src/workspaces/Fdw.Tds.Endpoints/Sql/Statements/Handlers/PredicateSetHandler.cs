using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Hosting;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// SET option ON|OFF for the session's ON/OFF options.
/// </summary>
/// <remarks>
/// <para>
/// NOCOUNT, QUOTED_IDENTIFIER, FMTONLY and NOEXEC change how this server answers, and are kept on the
/// session. The options a client sets at connect to SQL Server's own defaults — ANSI_WARNINGS, ANSI_PADDING,
/// ARITHABORT, CONCAT_NULL_YIELDS_NULL and the rest — are accepted, because this server already evaluates
/// every statement that way and nothing it reads changes under them.
/// </para>
/// <para>
/// ANSI_NULLS OFF would make "= NULL" match NULLs, which no store this server reads applies, and the
/// SHOWPLAN options ask for execution plans this server does not produce: both are refused rather than
/// accepted and ignored.
/// </para>
/// </remarks>
[TypeOption(typeof(SqlStatementHandlers), "PredicateSet", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PredicateSetHandler : SqlStatementHandlerBase<PredicateSetStatement>
{
    private static readonly Dictionary<SetOptions, Action<TdsSession, bool>> Kept = new()
    {
        [SetOptions.NoCount] = static (s, on) => s.NoCount = on,
        [SetOptions.QuotedIdentifier] = static (s, on) => s.QuotedIdentifier = on,
        [SetOptions.FmtOnly] = static (s, on) => s.FormatOnly = on,
        [SetOptions.NoExec] = static (s, on) => s.NoExec = on,
        [SetOptions.ParseOnly] = static (s, on) => s.NoExec = on,
    };

    private static readonly HashSet<SetOptions> Refused = [SetOptions.ShowPlanAll, SetOptions.ShowPlanText, SetOptions.ShowPlanXml, SetOptions.ForcePlan];

    /// <summary>Initializes a new instance of the <see cref="PredicateSetHandler"/> class.</summary>
    public PredicateSetHandler()
        : base("PredicateSet")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(PredicateSetStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        var options = Enum.GetValues<SetOptions>().Where(o => o != SetOptions.None && statement.Options.HasFlag(o)).ToList();
        var refused = options.Any(Refused.Contains) || (options.Contains(SetOptions.AnsiNulls) && !statement.IsOn);
        if (refused)
            return Refuse(SqlServerErrors.NotSupported(SqlText.Of(statement), statement.StartLine), context);

        foreach (var option in options)
        {
            if (Kept.TryGetValue(option, out var keep))
                keep(context.Session, statement.IsOn);
        }

        return Ran();
    }
}
