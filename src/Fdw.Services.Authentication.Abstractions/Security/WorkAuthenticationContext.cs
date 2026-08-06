using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Web.Http.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// A non-HTTP, work-scoped <see cref="IAuthenticationContext"/> that carries a tenant identifier from
/// the unit of work being executed (a scheduled pipeline run, an orchestration node execution, etc.)
/// rather than from a <see cref="System.Security.Claims.ClaimsPrincipal"/>.
/// </summary>
/// <remarks>
/// <para>
/// Why this type exists: the only <see cref="IAuthenticationContext"/> implementation before this one,
/// <see cref="ClaimsPrincipalAuthenticationContext"/>, requires an HTTP-authenticated
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> and therefore only ever exists on the HTTP
/// request path. A scheduled/background execution (dequeued by a <c>BackgroundService</c>) has no
/// <c>ClaimsPrincipal</c> at all, so without this type the ambient
/// <see cref="IAuthenticationContextAccessor.Current"/> is absent, and
/// <c>MsSqlConnection.BuildSessionContextPlan</c> resolves the connection to the reserved
/// deny-everywhere <see cref="AuthConstants.NoAccessPrincipalId"/> principal — a background execution
/// would see only shared/system rows, never its own tenant's data. This type gives background
/// executions a real, minimal <see cref="IAuthenticationContext"/> sourced directly from the
/// execution's own known <c>TenantId</c> (e.g. <c>ScheduleConfiguration.TenantId</c>,
/// <c>OrchestrationNodeConfiguration.TenantId</c>), so the same RLS session-context mechanism that
/// protects HTTP requests also protects background work — subject to the UserId caveat below.
/// </para>
/// <para>
/// <b>UserId caveat — OPEN SUB-PROBLEM (flagged, not silently patched):</b>
/// <c>MsSqlConnection.BuildSessionContextPlan</c> resolves <c>UserId</c> to the reserved
/// deny-everywhere <see cref="AuthConstants.NoAccessPrincipalId"/> principal whenever
/// <see cref="IAuthenticationContext.UserId"/> is not a parseable <see cref="Guid"/> — and, critically,
/// <c>TenantId</c> is ONLY set alongside a REAL resolved user identity, never alongside the
/// deny-everywhere principal (see <c>MsSqlConnection.SetUserSessionContext</c> remarks). When no
/// caller-specific user id is known, <see cref="UserId"/> falls back to the literal string
/// <c>"system"</c> (matching the existing <c>SystemAuditContextAccessor</c> convention for
/// background/system callers), which is NOT a parseable <see cref="Guid"/> and is NOT
/// <see cref="SystemAuthenticationContext"/> (<see cref="IsSystemContext"/> is <c>false</c> here). That
/// means a <see cref="WorkAuthenticationContext"/> constructed without an explicit
/// <c>userId</c>-equivalent resolves to the deny-everywhere principal — its
/// <see cref="ActiveTenantId"/> is simply discarded, and the execution sees only shared/system rows,
/// NOT "exactly its own tenant" as the constructor parameter might suggest. This is a real, load-bearing
/// gap: seeing exactly tenant X requires a principal that actually HOLDS tenant X's
/// <c>tenant.TenantOrgAccess</c> grant, which needs a real per-run Guid user id (most likely the
/// schedule's/execution's owning user) — a SEPARATE, unresolved decision about which principal a
/// background execution runs as. This is intentionally NOT silently patched with an invented default
/// Guid or a loosened gate (NO FALLBACKS WITHOUT EXPLICIT APPROVAL) — it remains open until the
/// owning-principal decision is made.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class WorkAuthenticationContext : IAuthenticationContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkAuthenticationContext"/> class.
    /// </summary>
    /// <param name="tenantId">The tenant identifier the unit of work belongs to.</param>
    /// <param name="userId">
    /// The identifier of the user/principal on whose behalf the work runs, if known (e.g. a schedule's
    /// owner). Defaults to <c>"system"</c> when not supplied — see the UserId caveat in the type remarks.
    /// </param>
    public WorkAuthenticationContext(Guid tenantId, string? userId = null)
    {
        ActiveTenantId = tenantId;
        UserId = userId ?? "system";
    }

    /// <inheritdoc/>
    public string UserId { get; }

    /// <inheritdoc/>
    // Why: Username has no separate identity source for work-scoped execution — mirror UserId so
    // display/log call sites always have a non-empty value.
    public string Username => UserId;

    /// <inheritdoc/>
    // Why: work-scoped execution carries no claims bag — it is sourced from the execution's own
    // TenantId, not from a token.
    public IDictionary<string, object> Claims { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <inheritdoc/>
    // Why: background executions carry no role assignments — authorization for background work is
    // enforced by tenant/RLS scoping, not by role checks.
    public IEnumerable<string> Roles { get; } = [];

    /// <inheritdoc/>
    // Why: background executions carry no baked permission set — they are not subject to the
    // effective-permission authorization path that HTTP requests go through.
    public IEnumerable<string> Permissions { get; } = [];

    /// <inheritdoc/>
    // Why: a unit of work with a known TenantId is, by construction, an authenticated/authorized
    // execution context — it is always true for this type.
    public bool IsAuthenticated => true;

    /// <inheritdoc/>
    // Why: "None" (no interactive authentication scheme) is the most neutral existing SecurityMethods
    // option for a non-interactive, work-scoped context — there is no dedicated "System"/"Internal"
    // SecurityMethodBase option in the framework today (grepped SecurityMethods: ApiKey, Certificate,
    // JWT, None, OAuth2).
    public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("None");

    /// <inheritdoc/>
    // Why: background work is not token-based, so there is no token expiry to track.
    public DateTimeOffset? ExpiresAt => null;

    /// <inheritdoc/>
    public Guid? ActiveTenantId { get; }

    /// <inheritdoc/>
    // Why: work-scoped execution always carries exactly one tenant (the execution's own) — there is no
    // org-level scoping concept for background work today.
    public Guid? ActiveOrgId => null;

    /// <inheritdoc/>
    // Why: a work context always carries a single, specific ActiveTenantId — it is never cross-tenant.
    public bool IsCrossTenant => false;

    /// <inheritdoc/>
    // Why: a work-scoped execution is bound to one tenant's own data, never a deliberate full-
    // visibility elevation. Only SystemAuthenticationContext reports true.
    public bool IsSystemContext => false;
}
