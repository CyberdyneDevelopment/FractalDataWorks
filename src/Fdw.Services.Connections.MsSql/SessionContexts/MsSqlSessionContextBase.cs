using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
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
/// code. Replacing this collection replaces that contract wholesale — including the system-bypass
/// behaviour, which is a property of <c>fn_TenantFilter</c>'s Mode 1 specifically and not of session
/// contexts in general. See <see cref="MsSqlSessionContextTypes"/> for the per-option contracts.
/// </para>
/// <para>
/// <b>Selection is exhaustive and mutually exclusive.</b> <see cref="Governs"/> partitions the space
/// of authentication contexts: exactly one option governs any given one, including <c>null</c>.
/// That is what lets <see cref="MsSqlSessionContextTypes.For"/> pick without an ordered if-else
/// chain and without a "none matched" fallback, and it is why the deny option's predicate is the
/// explicit complement of the other two rather than a catch-all.
/// </para>
/// </remarks>
public abstract class MsSqlSessionContextBase : TypeOptionBase<int, ISessionContext>, ISessionContext
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
    /// <param name="id">The unique identifier for this session context.</param>
    /// <param name="name">The name of this session context.</param>
    protected MsSqlSessionContextBase(int id, string name) : base(id, name)
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
    /// All five plan fields participate. <c>UserId</c> alone is insufficient: <c>TenantId</c> and
    /// <c>CrossTenant</c> select between the predicate's strict and cross-tenant modes, and
    /// <c>CanReadSecrets</c> gates restricted system rows — two callers identical but for that flag
    /// legitimately see different rows and must not share a cache entry.
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
            ":", plan.UserId.ToString(),
            ":", plan.TenantId.ToString(),
            ":", plan.IsCrossTenant ? "1" : "0",
            ":", plan.CanReadSecrets ? "1" : "0");
    }

    /// <summary>
    /// Writes exactly this session context's <c>SESSION_CONTEXT</c> keys onto an open connection.
    /// Called after every <c>OpenAsync</c> on a pooled connection.
    /// </summary>
    /// <param name="connection">The open connection to write the session context onto.</param>
    /// <param name="plan">The decision from <see cref="Plan"/> for the same authentication context.</param>
    /// <param name="logger">The logger of the connection performing the open.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public abstract Task Apply(
        SqlConnection connection,
        SessionContextPlan plan,
        ILogger logger,
        CancellationToken cancellationToken = default);

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
    /// against <c>tenant.TenantOrgAccess</c>.
    /// </summary>
    /// <param name="authenticationContext">The authentication context to test.</param>
    protected static bool IsResolvedUser(IAuthenticationContext? authenticationContext)
        => authenticationContext is { IsSystemContext: false, IsAuthenticated: true }
           && Guid.TryParse(authenticationContext.UserId, out _);

    /// <summary>
    /// Runs a <c>sp_set_session_context</c> batch on an open connection.
    /// </summary>
    /// <remarks>
    /// Why every caller sets <c>@read_only = 1</c>: it prevents the value being changed for the rest
    /// of the connection's lifetime, closing a privilege-escalation vector on a pooled connection.
    /// Why a single batch: it minimises round-trips and keeps the atomic setup window small.
    /// Why the failure rethrows rather than returning a result: a connection that has been opened
    /// but whose session context did not land is a connection the RLS predicate would evaluate
    /// against the wrong principal. It must not be handed back to the caller under any code path.
    /// </remarks>
    /// <param name="connection">The open connection to run the batch on.</param>
    /// <param name="sql">The batch text.</param>
    /// <param name="parameters">The parameters the batch text references.</param>
    /// <param name="logger">The logger of the connection performing the open.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    protected static async Task Execute(
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
        }
        catch (SqlException ex)
        {
            MsSqlConnectionLogger.TenantContextSetFailed(logger, ex.Message);
            throw;
        }
    }
}
