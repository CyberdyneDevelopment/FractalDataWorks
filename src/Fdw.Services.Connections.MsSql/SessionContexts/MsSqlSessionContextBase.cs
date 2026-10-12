using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.ErrorHandlers;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// Option base for <see cref="MsSqlSessionContextTypes"/> — the reference row-level-security
/// scheme's session contexts. Each option is one clause-level contract with
/// <c>security.fn_TenantFilter</c> <b>as deployed</b> in the <c>databases</c> repo.
/// </summary>
/// <remarks>
/// <para>
/// <b>These options are not neutral primitives.</b> They are the reference RLS design expressed as
/// code. Replacing this collection replaces that contract wholesale — including the system
/// elevation, which is a property of <c>fn_TenantFilter</c>'s <c>System</c> key specifically and not
/// of session contexts in general. See <see cref="MsSqlSessionContextTypes"/> for the per-option contracts.
/// </para>
/// <para>
/// <b>Selection is exhaustive and mutually exclusive.</b> <see cref="Governs"/> partitions the space
/// of authentication contexts: exactly one option governs any given one, including <c>null</c>.
/// That is what lets <see cref="MsSqlSessionContextTypes.For"/> pick without an ordered if-else
/// chain and without a "none matched" fallback, and it is why the deny option's predicate is the
/// explicit complement of the other two rather than a catch-all.
/// </para>
/// </remarks>
public abstract class MsSqlSessionContextBase : TypeOptionBase<ISessionContext>, ISessionContext
{
    /// <summary>
    /// Identifies this scheme in the cache-partition tokens its options produce, keeping them
    /// disjoint from every other scheme's.
    /// </summary>
    /// <remarks>
    /// Names the <i>scheme</i>, not the connection kind: a consumer talking to SQL Server under a
    /// different row-level-security design ships its own option base with its own prefix, and its
    /// tokens must never collide with these even though both kinds are "MsSql".
    /// </remarks>
    private const string SchemePartitionPrefix = "mssql-rls";

    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlSessionContextBase"/> class.
    /// </summary>
    /// <param name="name">The name of this session context.</param>
    protected MsSqlSessionContextBase(string name) : base(name)
    {
    }

    /// <summary>
    /// Gets a value indicating whether this session context governs <paramref name="authenticationContext"/>.
    /// </summary>
    /// <param name="authenticationContext">
    /// The authentication context of the current logical call flow, or <see langword="null"/> when
    /// none has been established.
    /// </param>
    public abstract bool Governs(IAuthenticationContext? authenticationContext);

    /// <summary>
    /// Builds the pure decision this session context carries for <paramref name="authenticationContext"/> —
    /// which <c>SESSION_CONTEXT</c> keys <see cref="Apply"/> will set. No SQL, no I/O, so the
    /// security-critical gate is unit-testable without a live SQL Server connection.
    /// </summary>
    /// <param name="authenticationContext">The authentication context this plan is built from.</param>
    public abstract SessionContextPlan Plan(IAuthenticationContext? authenticationContext);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Abstract because the three options differ, and the difference is exactly whether their branch
    /// of <c>security.fn_TenantFilter</c> joins a live table. The user branches read
    /// <c>tenant.UserTenantsConfiguration</c>, <c>tenant.TenantOrgAccess</c> or
    /// <c>security.VisibilityGroup</c> at query time, so an edit there changes what a caller sees
    /// without changing anything the partition can observe. The system branch and the shared-row
    /// branch join nothing and cannot drift.
    /// </para>
    /// </remarks>
    public abstract TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext);

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>Derived from <see cref="Plan"/>, and deliberately not overridable.</b> The plan already is
    /// the complete description of what this scheme tells the store about the caller — every
    /// <c>SESSION_CONTEXT</c> key <see cref="Apply"/> sets comes from it, and
    /// <c>security.fn_TenantFilter</c> reads exactly those keys and nothing else. So the visibility
    /// scope is a total function of the plan, and computing the partition from the plan makes the
    /// two incapable of disagreeing. An option that computed a partition independently could drift
    /// from the session it actually applies, which is precisely the defect this member exists to
    /// prevent; sealing it removes that possibility rather than relying on a test to catch it.
    /// </para>
    /// <para>
    /// Every plan field that reaches the connection participates. <c>UserId</c> alone is
    /// insufficient: <c>TenantId</c> decides which tenant a user sees (the global tenant sees all of
    /// them) — two callers identical but for it legitimately see different rows and must not share a
    /// cache entry.
    /// </para>
    /// <para>
    /// The scheme prefix keeps these tokens disjoint from any other scheme's. Option names are
    /// scheme-local (another scheme may also name an option <c>ForUser</c> and mean something
    /// different), so the prefix, not the name, is what makes the token globally unambiguous.
    /// </para>
    /// </remarks>
    public string CachePartition(IAuthenticationContext? authenticationContext)
    {
        var plan = Plan(authenticationContext);

        return string.Concat(
            SchemePartitionPrefix,
            ":", plan.IsSystem ? "1" : "0",
            ":", plan.IsTenantRun ? "1" : "0",
            ":", plan.UserId.ToString(),
            ":", plan.TenantId.ToString());
    }

    /// <summary>
    /// Gets the SQL and parameters that write exactly this session context's <c>SESSION_CONTEXT</c> keys.
    /// This is the one definition of what the context sets: <see cref="Apply"/> runs it on its own and
    /// <see cref="BatchedSessionContext"/> sends it in the same batch as a command.
    /// </summary>
    /// <param name="plan">The decision from <see cref="Plan"/> for the same authentication context.</param>
    /// <param name="parameterPrefix">
    /// Put in front of every parameter name, so that a statement sent in the same batch as a command
    /// cannot collide with that command's own parameters. Empty when the statement runs on its own.
    /// </param>
    /// <returns>The statement, with its parameters described but not bound to a command.</returns>
    public abstract SessionContextStatement Statement(SessionContextPlan plan, string parameterPrefix);

    /// <summary>
    /// Logs that this session context's keys were set on <paramref name="connection"/>. Kept apart from
    /// <see cref="Statement"/> because some of what it says (the deny principal's warning) is an
    /// operator's signal that must not be lost when the statement travels with a command, and must not
    /// be said before the keys are actually set.
    /// </summary>
    /// <param name="plan">The plan whose keys were set.</param>
    /// <param name="connection">The connection they were set on.</param>
    /// <param name="logger">The logger of the connection that set them.</param>
    public abstract void LogApplied(SessionContextPlan plan, SqlConnection connection, ILogger logger);

    /// <summary>
    /// Writes exactly this session context's <c>SESSION_CONTEXT</c> keys onto an open connection, as a
    /// batch of their own. Used by every path that runs no command with the context, or cannot carry it
    /// in the command's batch (a bulk copy, a stored procedure, a transaction that sets it once).
    /// </summary>
    /// <param name="connection">The open connection to write the session context onto.</param>
    /// <param name="plan">The decision from <see cref="Plan"/> for the same authentication context.</param>
    /// <param name="logger">The logger of the connection performing the open.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// Success once the keys are set; otherwise a failure, and the connection must not be used. Nothing is
    /// thrown for a database error: the failure carries <see cref="ContextFailureMessages"/>.
    /// </returns>
    public async Task<IGenericResult> Apply(
        SqlConnection connection,
        SessionContextPlan plan,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var statement = Statement(plan, parameterPrefix: string.Empty);

        var set = await Execute(
            connection,
            statement.Sql,
            statement.Parameters.Select(parameter => parameter.ToSqlParameter()).ToList(),
            logger,
            cancellationToken).ConfigureAwait(false);
        if (set.IsFailure)
            return set;

        LogApplied(plan, connection, logger);
        return GenericResult.Success();
    }

    /// <summary>
    /// The messages for a failure to set a session context: the one that says the context did not land,
    /// and then what the database said, through the handler for its error number. It is the only way a
    /// context failure is reported, whether the context was set on its own or in its command's batch.
    /// </summary>
    /// <param name="logger">The logger of the connection performing the open.</param>
    /// <param name="exception">The SQL exception the driver raised.</param>
    /// <param name="commandText">The SQL that was running when it was raised.</param>
    /// <returns>The messages for the failure; the one naming the context is first.</returns>
    public static IReadOnlyList<IGenericMessage> ContextFailureMessages(
        ILogger logger, SqlException exception, string commandText)
    {
        var messages = new List<IGenericMessage> { MsSqlConnectionLogger.TenantContextSetFailed(logger, exception.Message) };
        messages.AddRange(SqlErrorHandlers.FailureMessages(logger, exception, commandText));
        return messages;
    }

    /// <summary>
    /// Gets a value indicating whether <paramref name="authenticationContext"/> is an explicit
    /// system elevation.
    /// </summary>
    /// <remarks>
    /// Checked ahead of <see cref="IsResolvedUser"/> by every predicate that consults both: a
    /// <c>SystemAuthenticationContext</c>'s <c>UserId</c> ("system") is not <see cref="Guid"/>-parseable
    /// by design, so a Guid test applied first would send system elevation to the deny option.
    /// </remarks>
    /// <param name="authenticationContext">The authentication context to test.</param>
    protected static bool IsSystemElevation(IAuthenticationContext? authenticationContext)
        => authenticationContext is { IsSystemContext: true };

    /// <summary>
    /// Gets a value indicating whether <paramref name="authenticationContext"/> is a real,
    /// authenticated, <see cref="Guid"/>-identified user — the only shape that can be matched
    /// against a membership in <c>tenant.UserTenantsConfiguration</c>.
    /// </summary>
    /// <param name="authenticationContext">The authentication context to test.</param>
    protected static bool IsResolvedUser(IAuthenticationContext? authenticationContext)
        => authenticationContext is { IsSystemContext: false, IsAuthenticated: true }
           && authenticationContext is not TenantRunAuthenticationContext
           && Guid.TryParse(authenticationContext.UserId, out _);

    /// <summary>
    /// Gets a value indicating whether <paramref name="authenticationContext"/> is a tenant run: a
    /// tenant's work acting as no person, in exactly one tenant (FDW-1087). Never a resolved user, never
    /// a system elevation.
    /// </summary>
    /// <param name="authenticationContext">The authentication context to test.</param>
    protected static bool IsTenantRun(IAuthenticationContext? authenticationContext)
        => authenticationContext is TenantRunAuthenticationContext { IsSystemContext: false, ActiveTenantId: not null };

    /// <summary>
    /// Runs a <c>sp_set_session_context</c> batch on an open connection.
    /// </summary>
    /// <remarks>
    /// Why every caller sets <c>@read_only = 1</c>: it prevents the value being changed for the rest
    /// of the connection's lifetime, closing a privilege-escalation vector on a pooled connection.
    /// Why a single batch: it minimises round-trips and keeps the atomic setup window small.
    /// Why the failure is a result and not a throw: a database error is a fact about the database, not a
    /// fault in the application, so it is returned with the messages that say what happened. What does not
    /// change is that a connection whose session context did not land is one the RLS predicate would
    /// evaluate against the wrong principal, and must not be used: every caller checks the result, and
    /// returns its failure from inside the scope that disposes the connection.
    /// </remarks>
    /// <returns>Success once the batch has run; otherwise the failure from <see cref="ContextFailureMessages"/>.</returns>
    /// <param name="connection">The open connection to run the batch on.</param>
    /// <param name="sql">The batch text.</param>
    /// <param name="parameters">The parameters the batch text references.</param>
    /// <param name="logger">The logger of the connection performing the open.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    protected static async Task<IGenericResult> Execute(
        SqlConnection connection,
        string sql,
        IReadOnlyList<SqlParameter> parameters,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cmd = new SqlCommand(sql, connection) { CommandTimeout = 5 };
            await using (cmd.ConfigureAwait(false))
            {
                for (var i = 0; i < parameters.Count; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            return GenericResult.Success();
        }
        catch (SqlException ex)
        {
            return GenericResult.Failure(ContextFailureMessages(logger, ex, sql));
        }
    }
}
