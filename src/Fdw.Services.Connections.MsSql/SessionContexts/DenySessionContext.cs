using System.Collections.Generic;
using System.Data;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// No usable principal: sets <c>UserId</c> to the reserved
/// <see cref="AuthConstants.NoAccessPrincipalId"/> and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// Governs everything the other two options do not — no authentication context established at all,
/// an unauthenticated one, or one whose <c>UserId</c> is not <see cref="System.Guid"/>-parseable and
/// which is not an explicit system elevation.
/// </para>
/// <para>
/// <b>Its deny-ness is emergent, not declared.</b> <see cref="AuthConstants.NoAccessPrincipalId"/>
/// appears nowhere in the <c>databases</c> repo — no predicate, no policy, no seed row names it. It
/// denies purely because that Guid holds zero rows in <c>tenant.TenantOrgAccess</c>, so every
/// tenant-scoped branch of <c>security.fn_TenantFilter</c> fails its <c>EXISTS</c> check. A
/// different scheme, or the same scheme with that Guid granted access, gets a silently different
/// verdict from the identical option.
/// </para>
/// <para>
/// <b>This is not deny-everywhere.</b> <c>fn_TenantFilter.sql:48-51</c> admits shared/system rows
/// (<c>TenantId IS NULL AND VisibilityGroupId IS NULL</c>) with no session-context test at all, so
/// the deny principal still sees those — exactly as any other tenant-less caller does. What it is
/// denied is every <i>tenant-scoped</i> branch.
/// </para>
/// <para>
/// Why a real reserved principal rather than setting nothing: setting nothing is the Mode 1 system
/// bypass (see <see cref="SystemSessionContext"/>). Falling through to it for an unestablished
/// context would be fail-<i>open</i>. A non-null <c>UserId</c> that holds no grants fails closed
/// without needing the predicate to know anything about it.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlSessionContextTypes), "Deny")]
public sealed class DenySessionContext() : MsSqlSessionContextBase(3, "Deny")
{
    /// <inheritdoc />
    /// <remarks>
    /// The explicit complement of the other two predicates, so the three options partition every
    /// authentication context exhaustively and exclusively. Written out rather than left as a
    /// catch-all so the partition is checkable at this call site.
    /// </remarks>
    public override bool Governs(IAuthenticationContext? authenticationContext)
        => !IsSystemElevation(authenticationContext) && !IsResolvedUser(authenticationContext);

    /// <inheritdoc />
    public override SessionContextPlan Plan(IAuthenticationContext? authenticationContext)
        => SessionContextPlan.Deny;

    /// <inheritdoc />
    /// <remarks>
    /// Unbounded. The deny principal reaches only the shared-row branch
    /// (<c>fn_TenantFilter.sql:48-51</c>), which tests <c>TenantId IS NULL AND VisibilityGroupId IS
    /// NULL</c> on the row itself and joins nothing. Its emptiness in
    /// <c>tenant.TenantOrgAccess</c> is what denies it every tenant-scoped branch, and adding grants
    /// for a principal that exists in no table is not an operation — so what it sees cannot change
    /// out from under a cached result.
    /// </remarks>
    public override TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext)
        => TimeSpan.MaxValue;

    /// <inheritdoc />
    public override async Task Apply(
        SqlConnection connection,
        SessionContextPlan plan,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await Execute(
            connection,
            "EXEC sp_set_session_context @key = N'UserId', @value = @userId, @read_only = 1;",
            new List<SqlParameter>
            {
                new("@userId", SqlDbType.UniqueIdentifier) { Value = plan.UserId!.Value },
            },
            logger,
            cancellationToken).ConfigureAwait(false);

        MsSqlConnectionLogger.TraceNoAccessPrincipalContextSet(logger);
    }
}
