using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Logging;
using Fdw.Tds.Endpoints.Sql.Queries;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements;

/// <summary>
/// Parses a batch and runs its statements in order, ending each in its DONE token.
/// </summary>
/// <remarks>
/// <para>
/// The batch is parsed as SQL Server 2022 parses it, with QUOTED_IDENTIFIER as the session has it, so a
/// syntax error is error 102 with the parser's own message and line. The catalog is read once, the first
/// time a statement needs it.
/// </para>
/// <para>
/// A statement that fails ends the batch: its error goes to the client and the statements after it do not
/// run. Every DONE written here says more follows; the request ends in its own final DONE.
/// </para>
/// </remarks>
public static class SqlBatchRunner
{
    /// <summary>Parses a batch's text into its statements.</summary>
    /// <param name="text">The batch text.</param>
    /// <param name="context">The request.</param>
    /// <returns>The statements, or error 102.</returns>
    public static IGenericResult<IReadOnlyList<TSqlStatement>> Parse(string text, SqlExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(context);
        using var reader = new StringReader(text);
        var fragment = new TSql160Parser(context.Session.QuotedIdentifier).Parse(reader, out var errors);
        if (errors.Count > 0)
            return SqlRefusal.Refuse<IReadOnlyList<TSqlStatement>>(SqlServerErrors.Syntax(errors[0].Message, errors[0].Line), context.Logger);

        var statements = fragment is TSqlScript script
            ? script.Batches.SelectMany(b => b.Statements).ToList()
            : [];
        return GenericResult<IReadOnlyList<TSqlStatement>>.Success(statements);
    }

    /// <summary>Parses and runs a batch.</summary>
    /// <param name="text">The batch text.</param>
    /// <param name="context">The request.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>Success, or the refusal that ended the batch.</returns>
    public static async Task<IGenericResult> Run(string text, SqlExecutionContext context, CancellationToken cancellationToken = default)
    {
        var statements = Parse(text, context);
        return statements.IsFailure
            ? statements
            : await Run(statements.Value!, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs statements in order.</summary>
    /// <param name="statements">The statements.</param>
    /// <param name="context">The request.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>Success, or the refusal that ended them.</returns>
    public static async Task<IGenericResult> Run(IEnumerable<TSqlStatement> statements, SqlExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(statements);
        ArgumentNullException.ThrowIfNull(context);
        foreach (var statement in statements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context.Session.NoExec && !TurnsNoExecOff(statement))
                continue;

            var outcome = await RunOne(statement, context, cancellationToken).ConfigureAwait(false);
            if (outcome.IsFailure)
                return outcome;

            Done(context, outcome.Value!);
            var flushed = await context.Response.Flush().ConfigureAwait(false);
            if (flushed.IsFailure)
                return flushed;
        }

        return GenericResult.Success();
    }

    private static async Task<IGenericResult<SqlStatementOutcome>> RunOne(TSqlStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        if (FragmentSearch.ReadsCatalog(statement))
        {
            var catalog = await context.LoadCatalog(cancellationToken).ConfigureAwait(false);
            if (catalog.IsFailure)
                return catalog.ToNewResult<SqlStatementOutcome>();
        }

        if (!SqlStatementHandlers.TryFor(statement, out var handler))
            return SqlStatementHandlers.Refuse(statement, context.Session.Database, context.Logger);

        var started = Stopwatch.GetTimestamp();
        var outcome = await handler.Execute(statement, context, cancellationToken).ConfigureAwait(false);
        if (outcome.IsSuccess)
        {
            context.Session.LastRowCount = outcome.Value!.RowCount;
            TdsEndpointLog.StatementRan(context.Logger, context.Session.SessionId, statement.GetType().Name,
                outcome.Value.RowCount, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        return outcome;
    }

    private static void Done(SqlExecutionContext context, SqlStatementOutcome outcome)
    {
        if (!outcome.WritesDone)
            return;

        var status = TdsDoneStatus.More;
        if (outcome.Counted && !context.Session.NoCount)
            status |= TdsDoneStatus.Count;

        if (context.Session.TransactionCount > 0)
            status |= TdsDoneStatus.InTransaction;

        var token = context.InProcedure ? TdsTokenTypes.DoneInProcedure : TdsTokenTypes.Done;
        TdsTokenWriter.Done(context.Response.Payload, token, status, outcome.Command, outcome.RowCount);
    }

    // SET NOEXEC ON skips every statement but the one that turns it off again.
    private static bool TurnsNoExecOff(TSqlStatement statement)
        => statement is PredicateSetStatement { IsOn: false } set && set.Options.HasFlag(SetOptions.NoExec);
}
