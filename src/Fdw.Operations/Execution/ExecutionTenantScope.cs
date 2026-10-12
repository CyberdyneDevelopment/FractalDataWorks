using System;
using System.Collections.Generic;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Operations.Execution;

/// <summary>
/// Which tenants' execution items a caller reaches.
/// </summary>
/// <remarks>
/// <para>
/// The operational store has no row-level security, so the tracker applies the same answer
/// <c>security.fn_TenantFilter</c> gives configuration rows: a system context reaches every item; a caller
/// acting in the global tenant reaches every item; a caller acting in a tenant reaches that tenant's items;
/// anyone else reaches none.
/// </para>
/// <para>
/// Acting in the global tenant is read from the <c>globalTenant</c> claim, which is issued only on a token
/// whose tenant is the global one and only to a member of it, the same fact <c>IsPlatformAdmin</c> reads.
/// </para>
/// </remarks>
public sealed class ExecutionTenantScope
{
    private ExecutionTenantScope(bool reachesAll, Guid? tenantId)
    {
        ReachesAll = reachesAll;
        TenantId = tenantId;
    }

    /// <summary>Gets a value indicating whether the caller reaches every tenant's items.</summary>
    public bool ReachesAll { get; }

    /// <summary>Gets the one tenant the caller reaches, when it reaches exactly one.</summary>
    public Guid? TenantId { get; }

    /// <summary>Gets a value indicating whether the caller reaches no item at all.</summary>
    public bool ReachesNothing => !ReachesAll && TenantId is null;

    /// <summary>
    /// Works out the scope for a caller.
    /// </summary>
    /// <param name="caller">The current authentication context, or <see langword="null"/> when there is none.</param>
    /// <returns>The tenants the caller reaches.</returns>
    public static ExecutionTenantScope For(IAuthenticationContext? caller)
    {
        if (caller is null)
            return new ExecutionTenantScope(false, null);

        if (caller.IsSystemContext || ActsInGlobal(caller.Claims))
            return new ExecutionTenantScope(true, null);

        return new ExecutionTenantScope(false, caller.ActiveTenantId);
    }

    /// <summary>
    /// Whether the caller reaches an item of the given tenant.
    /// </summary>
    /// <param name="tenantId">The tenant the item belongs to.</param>
    /// <returns><see langword="true"/> when the caller reaches it.</returns>
    public bool Reaches(Guid tenantId) => ReachesAll || TenantId == tenantId;

    private static bool ActsInGlobal(IDictionary<string, object>? claims)
        => claims is not null
           && claims.TryGetValue(ClaimDefinitions.globalTenant.Name, out var value)
           && string.Equals(value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
}
