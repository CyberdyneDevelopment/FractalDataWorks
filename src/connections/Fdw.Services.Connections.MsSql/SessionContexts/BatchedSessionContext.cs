using System;
using System.Data;
using Fdw.Results;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// A session context sent in the same batch as the command it governs, instead of as a batch of its own
/// ahead of it.
/// </summary>
/// <remarks>
/// <para>
/// Setting the context on its own costs a round trip per pooled open, and a read is a round trip: so every
/// uncached read was two. Here the context's guarded statement is put in front of the command's own text, and
/// the context's parameters, under a reserved prefix, are added to the command's own, so the server sees one
/// batch and the command still runs on the same connection, after the keys are set, or not at all.
/// </para>
/// <para>
/// Only a plain-text command can carry it. A stored procedure's text is a procedure name, and a bulk copy
/// is not a command, so those keep the separate <see cref="MsSqlSessionContextBase.Apply"/>. The
/// caller decides whether a command is one of those; <see cref="Attach"/> refuses a command that is not
/// plain text rather than send something that would not run.
/// </para>
/// <para>
/// The command's text is changed in place. What was asked to run is the caller's to remember: log it
/// before attaching, and use that copy afterwards.
/// </para>
/// </remarks>
public sealed class BatchedSessionContext
{
    /// <summary>
    /// The prefix on every parameter name the context adds to a command. Reserved: a command that already
    /// uses it is refused, since the context's parameter would silently replace or duplicate the command's.
    /// </summary>
    public const string ParameterPrefix = "__fdwctx_";

    private const string ContextProcedure = "sp_set_session_context";

    private readonly MsSqlSessionContextBase _context;
    private readonly SessionContextPlan _plan;
    private readonly ILogger _logger;

    private BatchedSessionContext(MsSqlSessionContextBase context, SessionContextPlan plan, ILogger logger)
    {
        _context = context;
        _plan = plan;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="command"/> can carry the session context in its own
    /// batch: only a plain-text command can. A stored procedure's text is a procedure name, and a bulk copy
    /// (which reaches a connection as a marker on a stored-procedure command) is not a command at all.
    /// </summary>
    /// <param name="command">The translated command, before it runs.</param>
    /// <returns><see langword="true"/> when <see cref="Attach"/> will accept it.</returns>
    public static bool CanCarry(SqlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command.CommandType == CommandType.Text;
    }

    /// <summary>
    /// Puts the context's keys in front of <paramref name="command"/>, in the same batch.
    /// </summary>
    /// <param name="context">The session context that governs the calling principal.</param>
    /// <param name="plan">The plan <paramref name="context"/> made for that principal.</param>
    /// <param name="command">The translated command, before it runs. Its text and parameters are changed.</param>
    /// <param name="logger">The logger of the connection that will run the command.</param>
    /// <returns>
    /// The attached context, whose <see cref="Applied"/> the caller calls once the command has started
    /// without a context failure; a failure, with <paramref name="command"/> left as it was, when the
    /// command is not plain text or already uses a reserved parameter name.
    /// </returns>
    public static IGenericResult<BatchedSessionContext> Attach(
        MsSqlSessionContextBase context,
        SessionContextPlan plan,
        SqlCommand command,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(logger);

        if (!CanCarry(command))
        {
            return GenericResult<BatchedSessionContext>.Failure(
                MsSqlConnectionLogger.SessionContextInputInvalid(
                    logger, nameof(command.CommandType), $"{command.CommandType} cannot carry the session context in its batch"));
        }

        foreach (SqlParameter existing in command.Parameters)
        {
            if (existing.ParameterName.TrimStart('@').StartsWith(ParameterPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return GenericResult<BatchedSessionContext>.Failure(
                    MsSqlConnectionLogger.SessionContextInputInvalid(
                        logger, existing.ParameterName, $"the '{ParameterPrefix}' prefix is reserved for the session context"));
            }
        }

        var statement = context.Statement(plan, ParameterPrefix);

        command.CommandText = string.Concat(statement.Guarded, command.CommandText);
        foreach (var parameter in statement.Parameters)
        {
            command.Parameters.Add(parameter.ToSqlParameter());
        }

        return GenericResult<BatchedSessionContext>.Success(new BatchedSessionContext(context, plan, logger));
    }

    /// <summary>
    /// Logs that the keys were set, as <see cref="MsSqlSessionContextBase.Apply"/> does once it has set
    /// them. Called after the batch has started without a context failure, not before.
    /// </summary>
    /// <param name="connection">The connection the batch ran on.</param>
    public void Applied(SqlConnection connection) => _context.LogApplied(_plan, connection, _logger);

    /// <summary>
    /// Gets a value indicating whether <paramref name="exception"/> was raised by setting the context, not
    /// by the command that follows it: one of its errors came from <c>sp_set_session_context</c>.
    /// </summary>
    /// <param name="exception">The exception a batch built by <see cref="Attach"/> raised.</param>
    /// <returns><see langword="true"/> when the context could not be set, so the command never ran.</returns>
    public static bool IsContextFailure(SqlException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        foreach (SqlError error in exception.Errors)
        {
            if (IsContextProcedure(error.Procedure))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="procedure"/>, an error's procedure name, is the
    /// one that sets the context.
    /// </summary>
    /// <param name="procedure">The <see cref="SqlError.Procedure"/> of one error.</param>
    /// <returns><see langword="true"/> when it is <c>sp_set_session_context</c>.</returns>
    public static bool IsContextProcedure(string? procedure)
        => string.Equals(procedure, ContextProcedure, StringComparison.OrdinalIgnoreCase);
}
