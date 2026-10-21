using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Refuses a change that would leave the platform with no administrator.
/// </summary>
/// <remarks>
/// A platform administrator is a current, unexpired assignment of the administrator role (named by
/// <see cref="ISystemRoleConfiguration.AdminRoleName"/>) in the one tenant marked global. The global tenant is read from the
/// tenant store, never named here. Any other holder counts, including a service identity.
/// </remarks>
public sealed class PlatformAdministratorGuard : IPlatformAdministratorGuard
{
    private readonly ISystemRoleConfiguration _systemRoles;
    private readonly ITenantProvider _tenants;
    private readonly IRoleConfigurationProvider _roles;
    private readonly IUserRoleConfigurationProvider _userRoles;
    private readonly ILogger<PlatformAdministratorGuard> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlatformAdministratorGuard"/> class.
    /// </summary>
    /// <param name="systemRoles">Names the administrator role.</param>
    /// <param name="tenants">Finds the global tenant.</param>
    /// <param name="roles">Resolves the administrator role by name.</param>
    /// <param name="userRoles">Lists who holds it.</param>
    /// <param name="logger">The logger.</param>
    public PlatformAdministratorGuard(ISystemRoleConfiguration systemRoles, ITenantProvider tenants,
        IRoleConfigurationProvider roles, IUserRoleConfigurationProvider userRoles,
        ILogger<PlatformAdministratorGuard>? logger)
    {
        _systemRoles = systemRoles ?? throw new ArgumentNullException(nameof(systemRoles));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _userRoles = userRoles ?? throw new ArgumentNullException(nameof(userRoles));
        _logger = logger ?? NullLogger<PlatformAdministratorGuard>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> CanRemove(Guid userId, string roleName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!IsAdministratorRole(roleName))
            return GenericResult.Success();

        var global = await Global(cancellationToken).ConfigureAwait(false);
        if (!global.IsSuccess)
            return global;
        if (global.Value != tenantId)
            return GenericResult.Success();

        var held = await Holders(roleName, global.Value, cancellationToken).ConfigureAwait(false);
        if (!held.IsSuccess || held.Value is null)
            return held;

        if (!held.Value.Any(holder => IsUser(holder, userId)))
            return GenericResult.Success();

        var remaining = held.Value.Count(holder => !IsUser(holder, userId));
        if (remaining == 0)
            return GenericResult.Failure(PlatformAdministratorGuardLog.LastAdministrator(_logger, roleName, userId.ToString()));

        PlatformAdministratorGuardLog.AdministratorRemains(_logger, roleName, userId.ToString(), remaining);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult> CanDeleteRole(string roleName, CancellationToken cancellationToken = default)
    {
        if (!IsAdministratorRole(roleName))
            return GenericResult.Success();

        var global = await Global(cancellationToken).ConfigureAwait(false);
        if (!global.IsSuccess)
            return global;

        var held = await Holders(roleName, global.Value, cancellationToken).ConfigureAwait(false);
        if (!held.IsSuccess || held.Value is null)
            return held;

        return held.Value.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(PlatformAdministratorGuardLog.AdministratorRoleHeld(_logger, roleName, held.Value.Count));
    }

    private bool IsAdministratorRole(string roleName)
        => string.Equals(roleName, _systemRoles.AdminRoleName, StringComparison.OrdinalIgnoreCase);

    // The holder's UserId is text; a user's subject is their Guid in any spelling, a service identity's is a name.
    private static bool IsUser(UserRoleImplementationConfiguration holder, Guid userId)
        => Guid.TryParse(holder.UserId, out var held) && held == userId;

    private async Task<IGenericResult<Guid>> Global(CancellationToken cancellationToken)
    {
        var all = await _tenants.GetAllTenants(cancellationToken).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null)
            return GenericResult<Guid>.Failure(PlatformAdministratorGuardLog.ReadFailed(_logger, "the tenants", all.CurrentMessage));

        var marked = all.Value.Where(tenant => tenant.IsGlobal).ToList();
        return marked.Count == 1
            ? GenericResult<Guid>.Success(marked[0].Id)
            : GenericResult<Guid>.Failure(PlatformAdministratorGuardLog.GlobalTenantNotSingle(_logger, marked.Count));
    }

    private async Task<IGenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>> Holders(
        string roleName, Guid globalTenantId, CancellationToken cancellationToken)
    {
        var role = await _roles.Get(roleName, cancellationToken).ConfigureAwait(false);
        if (!role.IsSuccess)
            return GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Failure(
                PlatformAdministratorGuardLog.ReadFailed(_logger, "the role", role.CurrentMessage));
        if (role.Value is null)
            return GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Failure(
                PlatformAdministratorGuardLog.RoleNotFound(_logger, roleName));

        var roleId = role.Value.Id;
        var now = DateTimeOffset.UtcNow;
        var found = await _userRoles.Find<UserRoleImplementationConfiguration>(
            assignment => assignment.RoleId == roleId && assignment.TenantId == globalTenantId
                && assignment.IsCurrent && !assignment.IsDeleted
                && (assignment.ExpiresAt is null || assignment.ExpiresAt > now),
            cancellationToken).ConfigureAwait(false);
        if (!found.IsSuccess || found.Value is null)
            return GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Failure(
                PlatformAdministratorGuardLog.ReadFailed(_logger, "the administrators", found.CurrentMessage));

        return GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Success(found.Value.ToList());
    }
}
