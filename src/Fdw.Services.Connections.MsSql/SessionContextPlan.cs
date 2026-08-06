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
    private SessionContextPlan(bool isSystem, Guid? userId, Guid? tenantId, bool isCrossTenant, bool canReadSecrets)
    {
        IsSystem = isSystem;
        UserId = userId;
        TenantId = tenantId;
        IsCrossTenant = isCrossTenant;
        CanReadSecrets = canReadSecrets;
    }

    /// <summary>
    /// Gets a value indicating whether this plan is the explicit system-elevation plan — sets
    /// NOTHING at all (no SESSION_CONTEXT keys). The resulting NULL
    /// <c>SESSION_CONTEXT('UserId')</c> is what <c>security.fn_TenantFilter</c>'s Mode 1 checks for;
    /// there is no dedicated <c>SystemContext</c> key.
    /// </summary>
    public bool IsSystem { get; }

    /// <summary>
    /// Gets the <c>UserId</c> to set on the connection. Always non-null for a non-system plan — either
    /// a real, Guid-identified user's own id (<see cref="ForUser"/>), or the reserved
    /// deny-everywhere <see cref="AuthConstants.NoAccessPrincipalId"/> (<see cref="Deny"/>). Always
    /// null for the system plan.
    /// </summary>
    public Guid? UserId { get; }

    /// <summary>
    /// Gets the active tenant identifier. Only ever populated by <see cref="ForUser"/> — the
    /// deny-everywhere plan never carries one, even if the caller's <c>ActiveTenantId</c> was
    /// non-null, because <c>TenantId</c> is meaningless without a real resolved user identity to
    /// match against <c>tenant.TenantOrgAccess</c>.
    /// </summary>
    public Guid? TenantId { get; }

    /// <summary>
    /// Gets a value indicating whether <c>SESSION_CONTEXT('CrossTenant')</c> should be set to
    /// <c>N'1'</c>. Only meaningful when <see cref="TenantId"/> is null and this came from
    /// <see cref="ForUser"/> (never true for <see cref="Deny"/>).
    /// </summary>
    public bool IsCrossTenant { get; }

    /// <summary>
    /// Gets a value indicating whether <c>SESSION_CONTEXT('CanReadSecrets')</c> should be set to
    /// <c>N'1'</c>. Only ever true from <see cref="ForUser"/> (never true for <see cref="Deny"/>).
    /// </summary>
    public bool CanReadSecrets { get; }

    /// <summary>
    /// Gets the singleton plan for explicit system elevation — the ONLY plan that sets nothing.
    /// </summary>
    public static SessionContextPlan System { get; } = new(isSystem: true, userId: null, tenantId: null, isCrossTenant: false, canReadSecrets: false);

    /// <summary>
    /// Gets the singleton deny-everywhere plan: sets <c>UserId</c> to the reserved
    /// <see cref="AuthConstants.NoAccessPrincipalId"/> and nothing else. Used whenever no
    /// <c>IAuthenticationContext</c> is established at all, or the established one has no
    /// Guid-parseable <c>UserId</c> and is not an explicit system elevation.
    /// </summary>
    public static SessionContextPlan Deny { get; } = new(isSystem: false, userId: AuthConstants.NoAccessPrincipalId, tenantId: null, isCrossTenant: false, canReadSecrets: false);

    /// <summary>
    /// Builds the plan for an authenticated, Guid-identified real user.
    /// </summary>
    public static SessionContextPlan ForUser(Guid userId, Guid? tenantId, bool isCrossTenant, bool canReadSecrets)
        => new(isSystem: false, userId, tenantId, isCrossTenant, canReadSecrets);
}
