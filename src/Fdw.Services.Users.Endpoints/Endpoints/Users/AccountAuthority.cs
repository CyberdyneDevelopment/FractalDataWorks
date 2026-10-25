using System;
using System.Security.Claims;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Users.Configuration;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Who may act on somebody else's account, and who may act on their own.
/// </summary>
/// <remarks>
/// One rule, because seven routes ask it and a rule answered differently by any of them is a hole.
///
/// Administering an account means the platform administrator, or the Admin role held in the tenant
/// the account is homed in. Acting in that tenant is NOT enough on its own: every member of a
/// tenant acts in it, so that alone would let any colleague bind their own external identity to
/// somebody else's account, grant a first password to an account that has none, or mint a key that
/// authenticates as another person. A permission policy on the route is a separate gate and cannot
/// stand in for this one — a deployment is free to grant users:write to a role that is not Admin.
/// </remarks>
public static class AccountAuthority
{
    /// <summary>
    /// Answers whether the caller administers the given account.
    /// </summary>
    /// <param name="caller">The calling principal.</param>
    /// <param name="account">The account being acted on.</param>
    /// <param name="roles">Names the platform's roles and answers what the caller holds.</param>
    public static bool Administers(
        ClaimsPrincipal caller, IUserImplementationConfiguration account, ISystemRoleConfiguration roles)
    {
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(roles);

        if (roles.IsPlatformAdmin(caller))
            return true;

        if (!roles.IsInRole(caller, roles.AdminRoleName))
            return false;

        var actingIn = caller.FindFirst(ClaimDefinitions.tenantId.Name)?.Value;
        return Guid.TryParse(actingIn, out var actingTenantId) && account.TenantId == actingTenantId;
    }

    /// <summary>
    /// Answers whether the caller is the account itself.
    /// </summary>
    /// <param name="caller">The calling principal.</param>
    /// <param name="account">The account being acted on.</param>
    public static bool IsSelf(ClaimsPrincipal caller, IUserImplementationConfiguration account)
    {
        ArgumentNullException.ThrowIfNull(caller);
        ArgumentNullException.ThrowIfNull(account);

        var callerId = caller.FindFirstValue(ClaimTypes.NameIdentifier) ?? caller.FindFirstValue("sub");
        return Guid.TryParse(callerId, out var callerGuid) && callerGuid == account.Id;
    }

    /// <summary>
    /// Answers whether the caller may list, mint or revoke the account's API keys: the account
    /// itself, or somebody who administers it.
    /// </summary>
    /// <param name="caller">The calling principal.</param>
    /// <param name="account">The account whose keys are in question.</param>
    /// <param name="roles">Names the platform's roles and answers what the caller holds.</param>
    public static bool ManagesKeys(
        ClaimsPrincipal caller, IUserImplementationConfiguration account, ISystemRoleConfiguration roles)
        => IsSelf(caller, account) || Administers(caller, account, roles);

    /// <summary>
    /// Answers whether the caller may read the account's sign-ins: the account itself, or somebody who
    /// administers it.
    /// </summary>
    /// <param name="caller">The calling principal.</param>
    /// <param name="account">The account whose sign-ins are in question.</param>
    /// <param name="roles">Names the platform's roles and answers what the caller holds.</param>
    public static bool ReadsSignIns(
        ClaimsPrincipal caller, IUserImplementationConfiguration account, ISystemRoleConfiguration roles)
        => IsSelf(caller, account) || Administers(caller, account, roles);
}
