using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Default <see cref="IRolePermissionResolver"/> — expands role names to permission names through the
/// same <c>authz.Role</c> / <c>authz.RolePermission</c> / <c>authz.Permission</c> catalogue that
/// <see cref="EffectivePermissionResolver"/> reads for the user path.
/// </summary>
public sealed class RolePermissionResolver : IRolePermissionResolver
{
    private readonly IServiceConfigurationProvider<RoleConfiguration> _roleProvider;
    private readonly IServiceConfigurationProvider<PermissionConfiguration> _permissionProvider;
    private readonly IServiceConfigurationProvider<RolePermissionConfiguration> _rolePermissionProvider;
    private readonly ILogger<RolePermissionResolver> _logger;

    /// <summary>Initializes a new instance of the <see cref="RolePermissionResolver"/> class.</summary>
    /// <param name="roleProvider">Reads the role catalogue.</param>
    /// <param name="permissionProvider">Reads the permission catalogue.</param>
    /// <param name="rolePermissionProvider">Reads the role/permission junction.</param>
    /// <param name="logger">Optional logger.</param>
    public RolePermissionResolver(
        IServiceConfigurationProvider<RoleConfiguration> roleProvider,
        IServiceConfigurationProvider<PermissionConfiguration> permissionProvider,
        IServiceConfigurationProvider<RolePermissionConfiguration> rolePermissionProvider,
        ILogger<RolePermissionResolver>? logger)
    {
        _roleProvider = roleProvider ?? throw new ArgumentNullException(nameof(roleProvider));
        _permissionProvider = permissionProvider ?? throw new ArgumentNullException(nameof(permissionProvider));
        _rolePermissionProvider = rolePermissionProvider ?? throw new ArgumentNullException(nameof(rolePermissionProvider));
        _logger = logger ?? NullLogger<RolePermissionResolver>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyCollection<string>>> Resolve(
        IReadOnlyList<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        if (roleNames is null || roleNames.Count == 0)
            return GenericResult<IReadOnlyCollection<string>>.Failure(
                AuthorizationLog.RoleExpansionNamesRequired(_logger));

        var allRoles = await _roleProvider.Get(cancellationToken).ConfigureAwait(false);
        // Why the provider's own reason is carried through rather than restated: a catalogue read
        // fails for reasons that are not absence, and each says which. Reporting them all as "no such
        // role" sends the reader looking for a missing row when the row is there.
        if (!allRoles.IsSuccess)
            return allRoles.ToNewResult<IReadOnlyCollection<string>>();
        if (allRoles.Value is null)
            return GenericResult<IReadOnlyCollection<string>>.Failure(
                AuthorizationLog.RoleProviderQueryFailed(_logger));

        var allPermissions = await _permissionProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!allPermissions.IsSuccess)
            return allPermissions.ToNewResult<IReadOnlyCollection<string>>();
        if (allPermissions.Value is null)
            return GenericResult<IReadOnlyCollection<string>>.Failure(
                AuthorizationLog.PermissionProviderQueryFailed(_logger));

        var allRolePermissions = await _rolePermissionProvider.Get(cancellationToken).ConfigureAwait(false);
        if (!allRolePermissions.IsSuccess)
            return allRolePermissions.ToNewResult<IReadOnlyCollection<string>>();
        if (allRolePermissions.Value is null)
            return GenericResult<IReadOnlyCollection<string>>.Failure(
                AuthorizationLog.RolePermissionProviderQueryFailed(_logger));

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roleNames)
        {
            var role = allRoles.Value.FirstOrDefault(
                r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));

            // Why a failure and not a skip: a role name that matches nothing is a declaration pointing
            // at a row that does not exist. Skipping it grants a narrower set than declared and reads,
            // downstream, as a permission the operator forgot to grant.
            if (role is null)
                return GenericResult<IReadOnlyCollection<string>>.Failure(
                    AuthorizationLog.RoleNameUnknown(_logger, roleName));

            foreach (var rolePermission in allRolePermissions.Value.Where(rp => rp.RoleId == role.Id))
            {
                var permission = allPermissions.Value.FirstOrDefault(p => p.Id == rolePermission.PermissionId);
                if (permission is null)
                    return GenericResult<IReadOnlyCollection<string>>.Failure(
                        AuthorizationLog.RolePermissionUnresolved(
                            _logger, role.Name, rolePermission.PermissionId.ToString()));

                permissions.Add(permission.Name);
            }
        }

        AuthorizationLog.RolePermissionsExpanded(
            _logger, roleNames.Count, permissions.Count, string.Join(", ", roleNames));

        return GenericResult<IReadOnlyCollection<string>>.Success(permissions);
    }
}
