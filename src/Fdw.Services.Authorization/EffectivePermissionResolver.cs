using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Default implementation of <see cref="IEffectivePermissionResolver"/>.
/// Applies the global∪tenant∪org 3-tier union and returns the permission set for
/// the roles the user is ACTUALLY ASSIGNED — not the entire catalog.
/// Invoked by <c>BakePermissionsStepType</c> at token issuance and by
/// <c>ApiKeyAuthenticationHandler</c> to bake the permission set onto the caller.
/// </summary>
public sealed class EffectivePermissionResolver : IEffectivePermissionResolver
{
    private readonly IRoleConfigurationProvider _roleProvider;
    private readonly IPermissionConfigurationProvider _permissionProvider;
    private readonly IRolePermissionConfigurationProvider _rolePermissionProvider;
    private readonly IUserRoleConfigurationProvider _userRoleProvider;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly IOrgAccessProvider _orgAccessProvider;
    private readonly ILogger<EffectivePermissionResolver> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EffectivePermissionResolver"/>.
    /// </summary>
    public EffectivePermissionResolver(
        IRoleConfigurationProvider roleProvider,
        IPermissionConfigurationProvider permissionProvider,
        IRolePermissionConfigurationProvider rolePermissionProvider,
        IUserRoleConfigurationProvider userRoleProvider,
        IAuthenticationContextAccessor authContextAccessor,
        ILogger<EffectivePermissionResolver>? logger,
        IOrgAccessProvider? orgAccessProvider = null)
    {
        _roleProvider = roleProvider ?? throw new ArgumentNullException(nameof(roleProvider));
        _permissionProvider = permissionProvider ?? throw new ArgumentNullException(nameof(permissionProvider));
        _rolePermissionProvider = rolePermissionProvider ?? throw new ArgumentNullException(nameof(rolePermissionProvider));
        _userRoleProvider = userRoleProvider ?? throw new ArgumentNullException(nameof(userRoleProvider));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _logger = logger ?? NullLogger<EffectivePermissionResolver>.Instance;
        _orgAccessProvider = orgAccessProvider ?? NullOrgAccessProvider.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<EffectiveAuthorization>> Resolve(
        string userId,
        Guid? tenantId,
        Guid? orgId,
        bool isGlobalTenant,
        CancellationToken cancellationToken = default)
    {
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var catalogResult = await LoadCatalog(cancellationToken).ConfigureAwait(false);
        if (catalogResult is null)
            return GenericResult<EffectiveAuthorization>.Failure(AuthorizationLog.RoleProviderQueryFailed(_logger));

        var (allRoles, allPermissions, allRolePermissions) = catalogResult.Value;

        // A Guid stored as VARCHAR: compared as text the match depends on how each side spelled
        // it, so both are compared as the Guids they are (FDW-532). An unparseable subject matches
        // nothing, which is what it should do.
        _ = Guid.TryParse(userId, out var subjectId);
        var userRoleAssignmentsResult = await _userRoleProvider.Find<UserRoleImplementationConfiguration>(
                assignment => Guid.TryParse(assignment.UserId, out var assigned) && assigned == subjectId, cancellationToken).ConfigureAwait(false);
        if (!userRoleAssignmentsResult.IsSuccess || userRoleAssignmentsResult.Value is null)
            return GenericResult<EffectiveAuthorization>.Failure(
                AuthorizationLog.UserRoleAssignmentLoadFailed(_logger, userId));

        var userRoleAssignments = userRoleAssignmentsResult.Value;
        AuthorizationLog.UserRoleAssignmentsLoaded(_logger, userRoleAssignments.Count, userId);

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roleNameToId = allRoles.ToDictionary(r => r.Name, r => r.Id, StringComparer.OrdinalIgnoreCase);

        var globalCount = 0;
        var tenantCount = 0;

        if (userRoleAssignments.Count == 0)
        {
            AuthorizationLog.UserHasNoRoleAssignments(_logger, userId);
        }
        else
        {
            // Build a set of role IDs assigned to this user, scoped by tier.
            // Global assignments have TenantId == null.
            // Tenant-scoped assignments have TenantId matching the current tenant.
            var assignedRoleIds = new HashSet<Guid>(userRoleAssignments
                .Where(ur => ur.TenantId is null || (tenantId.HasValue && ur.TenantId == tenantId.Value) || isGlobalTenant)
                .Select(ur => ur.RoleId));

            (globalCount, tenantCount) = ApplyRoleTiers(
                userId, allRoles, allPermissions, allRolePermissions, roleNameToId,
                tenantId, isGlobalTenant, assignedRoleIds, permissions, roleNames);
        }

        var orgCount = await ApplyOrgTier(
            userId, tenantId, orgId, permissions, cancellationToken).ConfigureAwait(false);

        AuthorizationLog.ThreeTierPermissionsResolved(_logger, globalCount, tenantCount, orgCount, permissions.Count, userId);

        return GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(permissions, roleNames));
    }

    private async Task<(IReadOnlyList<IRoleImplementationConfiguration>, IReadOnlyList<IPermissionImplementationConfiguration>, IReadOnlyList<IRolePermissionImplementationConfiguration>)?> LoadCatalog(
        CancellationToken cancellationToken)
    {
        var allRolesResult = await _roleProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!allRolesResult.IsSuccess || allRolesResult.Value is null)
        {
            AuthorizationLog.RoleProviderQueryFailed(_logger);
            return null;
        }

        var allPermissionsResult = await _permissionProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!allPermissionsResult.IsSuccess || allPermissionsResult.Value is null)
        {
            AuthorizationLog.PermissionProviderQueryFailed(_logger);
            return null;
        }

        var allRolePermissionsResult = await _rolePermissionProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!allRolePermissionsResult.IsSuccess || allRolePermissionsResult.Value is null)
        {
            AuthorizationLog.RolePermissionProviderQueryFailed(_logger);
            return null;
        }

        return (allRolesResult.Value, allPermissionsResult.Value, allRolePermissionsResult.Value);
    }

    private (int GlobalCount, int TenantCount) ApplyRoleTiers(
        string userId,
        IReadOnlyList<IRoleImplementationConfiguration> allRoles,
        IReadOnlyList<IPermissionImplementationConfiguration> allPermissions,
        IReadOnlyList<IRolePermissionImplementationConfiguration> allRolePermissions,
        Dictionary<string, Guid> roleNameToId,
        Guid? currentTenantId,
        bool isGlobalTenant,
        HashSet<Guid> assignedRoleIds,
        HashSet<string> permissions,
        HashSet<string> roleNames)
    {
        var globalPermCount = 0;
        var tenantPermCount = 0;

        AuthorizationLog.UserRolesSelected(_logger, assignedRoleIds.Count, userId, allRoles.Count);

        foreach (var role in allRoles)
        {
            if (!assignedRoleIds.Contains(role.Id))
                continue;

            var roleIsGlobal = !role.IsTenantScoped;
            if (!roleIsGlobal && !RoleContributesToTenant(role, currentTenantId, isGlobalTenant))
                continue;

            roleNames.Add(role.Name);

            var rolePerms = allRolePermissions.Where(rp => rp.RoleId == role.Id).ToList();
            AuthorizationLog.RolePermissionsMatched(_logger, role.Name, role.Id.ToString(), rolePerms.Count);

            var loggedMismatch = false;
            for (var i = 0; i < rolePerms.Count; i++)
            {
                var permConfig = FindPermission(allPermissions, rolePerms[i].PermissionId);
                if (permConfig is not null)
                {
                    if (permissions.Add(permConfig.Name))
                    {
                        if (roleIsGlobal) globalPermCount++;
                        else tenantPermCount++;
                    }
                }
                else if (!loggedMismatch)
                {
                    var sampleId = allPermissions.Count > 0 ? allPermissions[0].Id.ToString() : "(none)";
                    AuthorizationLog.PermissionIdUnresolved(_logger, rolePerms[i].PermissionId.ToString(), sampleId);
                    loggedMismatch = true;
                }
            }
        }

        return (globalPermCount, tenantPermCount);
    }

    private async Task<int> ApplyOrgTier(
        string userId,
        Guid? currentTenantId,
        Guid? orgId,
        HashSet<string> permissions,
        CancellationToken cancellationToken)
    {
        if (!orgId.HasValue || !currentTenantId.HasValue)
        {
            AuthorizationLog.OrgTierSkippedNoOrgContext(_logger, userId);
            return 0;
        }

        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            AuthorizationLog.OrgTierSkippedNonGuidUserId(_logger, userId);
            return 0;
        }

        var orgGrantsResult = await _orgAccessProvider.Get(
            userIdGuid, currentTenantId.Value, orgId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (!orgGrantsResult.IsSuccess || orgGrantsResult.Value is null)
            return 0;

        var orgPermCount = 0;
        foreach (var grant in orgGrantsResult.Value)
        {
            orgPermCount += ApplyOrgGrant(grant, permissions);
        }
        return orgPermCount;
    }

    /// <summary>Adds the permission an org-tier grant confers.</summary>
    /// <remarks>
    /// A grant's RoleName is not read. It is descriptive on this table -- it records why the
    /// access was granted, and the RLS predicate that consumes these rows tests their existence
    /// rather than either name. The values seeded into it (PlatformAdmin, TenantMember) are not
    /// in the role catalogue and never were, so the lookup that used to sit here missed for
    /// every row and contributed nothing, silently, while PermissionName carried the tier.
    /// Restoring it means deciding those role names are real and seeding them; until then a
    /// lookup that cannot succeed is worse than no lookup, because it reads as one that works.
    /// </remarks>
    private static int ApplyOrgGrant(
        TenantOrgAccessConfiguration grant,
        HashSet<string> permissions)
        => !string.IsNullOrEmpty(grant.PermissionName) && permissions.Add(grant.PermissionName)
            ? 1
            : 0;

    private static bool RoleContributesToTenant(IRoleImplementationConfiguration? roleDef, Guid? currentTenantId, bool isGlobalTenant)
        => isGlobalTenant
           || (roleDef is not null && roleDef.IsTenantScoped
               && currentTenantId.HasValue
               && roleDef.TenantId == currentTenantId.Value);

    private static IPermissionImplementationConfiguration? FindPermission(IReadOnlyList<IPermissionImplementationConfiguration> permissions, Guid permissionId)
    {
        for (var i = 0; i < permissions.Count; i++)
        {
            if (permissions[i].Id == permissionId)
                return permissions[i];
        }
        return null;
    }
}
