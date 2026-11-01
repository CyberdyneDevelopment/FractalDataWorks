using System.Collections.Generic;
using System.Data;
using System;
using System.Diagnostics.CodeAnalysis;
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
/// denies purely because that Guid holds no membership in <c>tenant.UserTenantsConfiguration</c>,
/// so every tenant-scoped branch of <c>security.fn_TenantFilter</c> fails its <c>EXISTS</c> check. A
/// different scheme, or the same scheme with that Guid given a membership, gets a silently
/// different verdict from the identical option.
/// </para>
/// <para>
/// <b>This is not deny-everywhere.</b> <c>fn_TenantFilter</c> admits shared rows — owned by the
/// global or the default tenant, with no visibility group — to any identified caller, so the deny
/// principal still sees those, exactly as any other tenant-less caller does. What it is denied is
/// every <i>tenant-scoped</i> branch, and every write.
/// </para>
/// <para>
/// Why a real reserved principal rather than setting nothing: a connection that sets nothing now
/// sees nothing at all (system is the explicit <c>System</c> key, see
/// <see cref="SystemSessionContext"/>), which would hide the shared rows an anonymous request is
/// entitled to — the platform's own themes, settings and reference data. A non-null <c>UserId</c>
/// with no memberships sees exactly those and fails closed on everything else, without the
/// predicate needing to know anything about it.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlSessionContextTypes), "Deny")]
public sealed class DenySessionContext() : MsSqlSessionContextBase("Deny")
{
    /// <inheritdoc />
    /// <remarks>
    /// The explicit complement of the other two predicates, so the three options partition every
    /// authentication context exhaustively and exclusively. Written out rather than left as a
    /// catch-all so the partition is checkable at this call site.
    /// </remarks>
    public override bool Governs(IAuthenticationContext? authenticationContext)
        => !IsSystemElevation(authenticationContext)
           && !IsResolvedUser(authenticationContext)
           && !IsTenantRun(authenticationContext);

    /// <inheritdoc />
    public override SessionContextPlan Plan(IAuthenticationContext? authenticationContext)
        => SessionContextPlan.Deny(DescribeReason(authenticationContext));

    /// <summary>
    /// Names which of <see cref="Governs"/>'s two sub-cases actually applied — <c>IsResolvedUser</c>
    /// is itself two checks (authenticated, and <c>UserId</c> Guid-parseable), collapsed into one
    /// boolean there. <c>IsSystemElevation</c> is already ruled out by the time this runs (only
    /// called when <see cref="Governs"/> returned true), so this only distinguishes the other two.
    /// </summary>
    private static string DescribeReason(IAuthenticationContext? authenticationContext)
    {
        if (authenticationContext is null)
            return "no IAuthenticationContext was established for this connection";

        if (!authenticationContext.IsAuthenticated)
            return $"context for UserId '{authenticationContext.UserId}' is not authenticated";

        return $"authenticated context's UserId '{authenticationContext.UserId}' is not Guid-parseable";
    }

    /// <inheritdoc />
    /// <remarks>
    /// Unbounded. The deny principal reaches only the shared-row branch of <c>fn_TenantFilter</c>,
    /// which tests the row's own <c>TenantId</c> and <c>VisibilityGroupId</c> and joins nothing. Its
    /// absence from <c>tenant.UserTenantsConfiguration</c> is what denies it every tenant-scoped
    /// branch, and giving a membership to a principal that exists in no table is not an operation —
    /// so what it sees cannot change out from under a cached result.
    /// </remarks>
    public override TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext)
        => TimeSpan.MaxValue;

    /// <inheritdoc />
    public override SessionContextStatement Statement(SessionContextPlan plan, string parameterPrefix)
    {
        var userId = string.Concat(parameterPrefix, "userId");

        return new SessionContextStatement(
            $"EXEC sp_set_session_context @key = N'UserId', @value = @{userId}, @read_only = 1;",
            new List<SessionContextParameter>
            {
                new(userId, SqlDbType.UniqueIdentifier, plan.UserId!.Value),
            });
    }

    /// <inheritdoc />
    public override void LogApplied(SessionContextPlan plan, SqlConnection connection, ILogger logger)
        => MsSqlConnectionLogger.NoAccessPrincipalContextSet(logger, connection.DataSource, connection.Database, plan.DenyReason);
}
