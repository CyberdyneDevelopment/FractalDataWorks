using System;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// The decision of WHICH SESSION_CONTEXT keys <c>MsSqlConnection.SetUserSessionContext</c>
/// should set on a pooled connection, computed from the connection's <c>IAuthenticationContext</c>.
/// </summary>
/// <remarks>
/// <para>
/// Why this is a separate type: the SYSTEM vs USER vs DENY decision is the security-critical gate
/// the context-layer tenant-deny design relies on, and is worth unit testing without opening a live
/// SQL Server connection. Keeping the pure decision (no SQL, no I/O) in this type lets it be tested
/// directly, while each <see cref="MsSqlSessionContextTypes"/> option owns building and executing
/// the SQL batch for the plan it carries.
/// </para>
/// <para>
/// <b>Shipping in <c>Fdw.Services.Connections.MsSql</c> does not make this scheme canonical.</b>
/// This type and the <see cref="MsSqlSessionContextTypes"/> options that carry it are the
/// <i>reference</i> row-level-security design — a contract with <c>security.fn_TenantFilter</c> as
/// deployed, not a neutral primitive of SQL Server connectivity. They live beside the connection
/// implementation for availability, not for endorsement. A consumer running a different scheme
/// replaces the whole collection (see <see cref="MsSqlSessionContextTypes"/>) and inherits none of
/// these assumptions.
/// </para>
/// </remarks>
public readonly struct SessionContextPlan
{
    private SessionContextPlan(bool isSystem, Guid? userId, Guid? tenantId, string denyReason, bool isTenantRun = false)
    {
        IsSystem = isSystem;
        IsTenantRun = isTenantRun;
        UserId = userId;
        TenantId = tenantId;
        DenyReason = denyReason;
    }

    /// <summary>
    /// Gets a value indicating whether this plan is an explicit system elevation, which sets
    /// <c>SESSION_CONTEXT('System') = '1'</c>. System is a positive assertion: a connection that
    /// sets no keys at all sees nothing under <c>security.fn_TenantFilter</c> (FDW-825).
    /// </summary>
    public bool IsSystem { get; }

    /// <summary>Gets whether this plan is a tenant run: a tenant's work acting as no person (FDW-1087).</summary>
    public bool IsTenantRun { get; }

    /// <summary>
    /// Gets the <c>UserId</c> to set on the connection. Always non-null for a non-system plan — either
    /// a real, Guid-identified user's own id (<see cref="ForUser"/>), or the reserved
    /// deny-everywhere <see cref="AuthConstants.NoAccessPrincipalId"/> (<see cref="Deny"/>). Always
    /// null for a system plan.
    /// </summary>
    public Guid? UserId { get; }

    /// <summary>
    /// Gets the tenant the caller is acting in. For <see cref="ForUser"/> it is the token's tenant and
    /// decides what the connection sees. For <see cref="ForSystem"/> it is the tenant a system
    /// elevation is working for — a background run for one tenant's pipeline — and decides only which
    /// tenant new rows default to; a system connection sees everything regardless. The deny plan
    /// never carries one.
    /// </summary>
    public Guid? TenantId { get; }

    /// <summary>
    /// Gets which of <see cref="DenySessionContext"/>'s three governing cases produced this plan —
    /// no authentication context established at all, an unauthenticated one, or one whose
    /// <c>UserId</c> is not <see cref="Guid"/>-parseable. Empty for system and user plans, which are
    /// never governed by that decision.
    /// </summary>
    public string DenyReason { get; }

    /// <summary>
    /// Builds the plan for an explicit system elevation.
    /// </summary>
    /// <param name="tenantId">
    /// The tenant the elevation is working for, when it is working for one — the owning tenant of a
    /// background run. Null when the work belongs to no tenant (host startup, platform reads).
    /// </param>
    public static SessionContextPlan ForSystem(Guid? tenantId)
        => new(isSystem: true, userId: null, tenantId, denyReason: string.Empty);

    /// <summary>
    /// Builds the deny-everywhere plan: sets <c>UserId</c> to the reserved
    /// <see cref="AuthConstants.NoAccessPrincipalId"/> and nothing else. Used whenever no
    /// <c>IAuthenticationContext</c> is established at all, or the established one has no
    /// Guid-parseable <c>UserId</c> and is not an explicit system elevation.
    /// </summary>
    /// <param name="reason">
    /// Which governing case produced this plan — carried through to the connection-open warning so
    /// the log names the actual cause instead of a single hardcoded guess.
    /// </param>
    public static SessionContextPlan Deny(string reason)
        => new(isSystem: false, userId: AuthConstants.NoAccessPrincipalId, tenantId: null, denyReason: reason);

    /// <summary>
    /// Builds the plan for an authenticated, Guid-identified real user acting in
    /// <paramref name="tenantId"/>.
    /// </summary>
    public static SessionContextPlan ForUser(Guid userId, Guid? tenantId)
        => new(isSystem: false, userId, tenantId, denyReason: string.Empty);

    /// <summary>
    /// The plan for a tenant run: its principal and the one tenant it acts in, which row-level security's
    /// tenant-run branch admits to that tenant's own rows.
    /// </summary>
    /// <param name="principalId">The run's principal (the pipeline's id).</param>
    /// <param name="tenantId">The tenant the run acts in.</param>
    /// <returns>The plan.</returns>
    public static SessionContextPlan ForTenantRun(Guid principalId, Guid tenantId)
        => new(isSystem: false, principalId, tenantId, denyReason: string.Empty, isTenantRun: true);
}
