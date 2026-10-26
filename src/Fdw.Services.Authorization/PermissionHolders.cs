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
/// Default implementation of <see cref="IPermissionHolders"/>.
/// </summary>
/// <remarks>
/// Reads the role catalogue and the assignments as the system, the way <see cref="UserPermissions"/> does: the people to tell are not the caller's to see, and
/// the global tenant's assignments are not visible from another tenant. The roles that count and the assignments that count are the ones
/// <see cref="UserPermissions"/> counts when it bakes a token.
/// </remarks>
public sealed class PermissionHolders : IPermissionHolders
{
    private readonly IRoleConfigurationProvider _roles;
    private readonly IPermissionConfigurationProvider _permissions;
    private readonly IRolePermissionConfigurationProvider _rolePermissions;
    private readonly IUserRoleConfigurationProvider _userRoles;
    private readonly ITenantProvider _tenants;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly ILogger<PermissionHolders> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionHolders"/> class.
    /// </summary>
    /// <param name="roles">Reads the roles.</param>
    /// <param name="permissions">Reads the permissions.</param>
    /// <param name="rolePermissions">Reads what each role carries.</param>
    /// <param name="userRoles">Reads who holds each role.</param>
    /// <param name="tenants">Finds the global tenant.</param>
    /// <param name="authContext">The accessor the system scope is opened on.</param>
    /// <param name="logger">The logger.</param>
    public PermissionHolders(
        IRoleConfigurationProvider roles,
        IPermissionConfigurationProvider permissions,
        IRolePermissionConfigurationProvider rolePermissions,
        IUserRoleConfigurationProvider userRoles,
        ITenantProvider tenants,
        IAuthenticationContextAccessor authContext,
        ILogger<PermissionHolders>? logger)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _rolePermissions = rolePermissions ?? throw new ArgumentNullException(nameof(rolePermissions));
        _userRoles = userRoles ?? throw new ArgumentNullException(nameof(userRoles));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _logger = logger ?? NullLogger<PermissionHolders>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<Guid>>> Get(string permissionName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        using var systemScope = new SystemAuthenticationContextScope(_authContext);
        return await HoldersIn(permissionName, tenantId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<Guid>>> GetOnPlatform(string permissionName, CancellationToken cancellationToken = default)
    {
        using var systemScope = new SystemAuthenticationContextScope(_authContext);

        var tenants = await _tenants.GetAllTenants(cancellationToken).ConfigureAwait(false);
        if (!tenants.IsSuccess || tenants.Value is null)
            return GenericResult<IReadOnlyList<Guid>>.Failure(PermissionHoldersLog.ReadFailed(_logger, permissionName, "the tenants", tenants.CurrentMessage));

        var marked = tenants.Value.Where(tenant => tenant.IsGlobal).ToList();
        return marked.Count == 1
            ? await HoldersIn(permissionName, marked[0].Id, cancellationToken).ConfigureAwait(false)
            : GenericResult<IReadOnlyList<Guid>>.Failure(PermissionHoldersLog.GlobalTenantNotSingle(_logger, permissionName, marked.Count));
    }

    private async Task<IGenericResult<IReadOnlyList<Guid>>> HoldersIn(string permissionName, Guid tenantId, CancellationToken cancellationToken)
    {
        var roleIds = await RolesCarrying(permissionName, tenantId, cancellationToken).ConfigureAwait(false);
        if (!roleIds.IsSuccess || roleIds.Value is null)
            return roleIds.ToNewResult<IReadOnlyList<Guid>>();

        var carrying = roleIds.Value;
        var now = DateTimeOffset.UtcNow;
        var assignments = await _userRoles.Find<UserRoleImplementationConfiguration>(
            assignment => carrying.Contains(assignment.RoleId) && assignment.TenantId == tenantId
                && assignment.IsCurrent && !assignment.IsDeleted
                && (assignment.ExpiresAt is null || assignment.ExpiresAt > now),
            cancellationToken).ConfigureAwait(false);
        if (!assignments.IsSuccess || assignments.Value is null)
            return GenericResult<IReadOnlyList<Guid>>.Failure(PermissionHoldersLog.ReadFailed(_logger, permissionName, "the role assignments", assignments.CurrentMessage));

        // Why only the subjects that parse: a person's subject is their Guid, a service identity's is the name it was declared with, and a service is not
        // someone to tell.
        var people = assignments.Value
            .Select(assignment => Guid.TryParse(assignment.UserId, out var person) ? person : (Guid?)null)
            .OfType<Guid>()
            .Distinct()
            .ToList();

        PermissionHoldersLog.HoldersResolved(_logger, people.Count, permissionName, tenantId.ToString("D"));
        return GenericResult<IReadOnlyList<Guid>>.Success(people);
    }

    // The roles that count in the tenant: a platform role counts anywhere, a tenant's own role only in its tenant.
    private async Task<IGenericResult<HashSet<Guid>>> RolesCarrying(string permissionName, Guid tenantId, CancellationToken cancellationToken)
    {
        var permissions = await _permissions.Get(cancellationToken).ConfigureAwait(false);
        if (!permissions.IsSuccess || permissions.Value is null)
            return GenericResult<HashSet<Guid>>.Failure(PermissionHoldersLog.ReadFailed(_logger, permissionName, "the permissions", permissions.CurrentMessage));

        var permission = permissions.Value.FirstOrDefault(candidate => string.Equals(candidate.Name, permissionName, StringComparison.OrdinalIgnoreCase));
        if (permission is null)
            return GenericResult<HashSet<Guid>>.Failure(PermissionHoldersLog.PermissionNotFound(_logger, permissionName));

        var roles = await _roles.Get(cancellationToken).ConfigureAwait(false);
        if (!roles.IsSuccess || roles.Value is null)
            return GenericResult<HashSet<Guid>>.Failure(PermissionHoldersLog.ReadFailed(_logger, permissionName, "the roles", roles.CurrentMessage));

        var grants = await _rolePermissions.Get(cancellationToken).ConfigureAwait(false);
        if (!grants.IsSuccess || grants.Value is null)
            return GenericResult<HashSet<Guid>>.Failure(PermissionHoldersLog.ReadFailed(_logger, permissionName, "what the roles carry", grants.CurrentMessage));

        // Why PermissionCoverage and not the permission's id: enforcement asks PermissionCoverage.Covers over the names a token carries, so a role
        // that carries `resource:*` passes the endpoint's check for `resource:read`. The lookup must agree with it.
        var namesById = permissions.Value.ToLookup(candidate => candidate.Id, candidate => candidate.Name);
        var carrying = roles.Value
            .Where(role => !role.IsTenantScoped || role.TenantId == tenantId)
            .Where(role => PermissionCoverage.Covers(
                RoleOwnedGrants.Of(role, grants.Value, _logger).SelectMany(grant => namesById[grant.PermissionId]),
                permissionName))
            .Select(role => role.Id)
            .ToHashSet();
        return GenericResult<HashSet<Guid>>.Success(carrying);
    }
}
