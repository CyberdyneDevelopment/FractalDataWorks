using System.Collections.Generic;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>
/// The grants that count for a role: the role/permission rows owned by the role's own tenant.
/// </summary>
/// <remarks>
/// <para>
/// A role is owned by one tenant (<see cref="IRoleImplementationConfiguration.TenantId"/>): a tenant's own
/// role by that tenant, a role shared by every tenant by the shared tenant the platform seeds it in. What a
/// role grants is what its owner wrote. A grant row owned by any other tenant is not counted, because the
/// role/permission catalogue is read across every tenant: counting it would let one tenant change what a
/// shared role grants in every other tenant.
/// </para>
/// <para>
/// A role with no owning tenant recorded counts no grants, and says so. There is no tenant to assume.
/// </para>
/// <para>
/// Both resolvers read through this one rule: <see cref="UserPermissions"/> for a user and
/// <see cref="RoleGrants"/> for an identity that names its roles.
/// </para>
/// </remarks>
internal static class RoleOwnedGrants
{
    /// <summary>Returns the grants of <paramref name="role"/> that its owning tenant wrote.</summary>
    /// <param name="role">The role.</param>
    /// <param name="grants">Every role/permission row, of every role and tenant.</param>
    /// <param name="logger">Logs each grant that is not counted.</param>
    /// <returns>The counted grants, in catalogue order.</returns>
    public static List<IRolePermissionImplementationConfiguration> Of(
        IRoleImplementationConfiguration role,
        IReadOnlyList<IRolePermissionImplementationConfiguration> grants,
        ILogger logger)
    {
        var counted = new List<IRolePermissionImplementationConfiguration>();
        if (role.TenantId is not { } owner)
        {
            AuthorizationLog.RoleTenantNotRecorded(logger, role.Name);
            return counted;
        }

        for (var i = 0; i < grants.Count; i++)
        {
            var grant = grants[i];
            if (grant.RoleId != role.Id)
                continue;

            if (grant.TenantId == owner)
            {
                counted.Add(grant);
                continue;
            }

            AuthorizationLog.RolePermissionOutsideRoleTenant(
                logger, role.Name, owner.ToString("D"), grant.PermissionId.ToString("D"),
                grant.TenantId is { } tenant ? tenant.ToString("D") : "(none)");
        }

        return counted;
    }
}
