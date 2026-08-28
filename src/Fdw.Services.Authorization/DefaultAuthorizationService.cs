using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Default implementation of <see cref="IFrameworkAuthorizationService"/>.
/// Enforces against the baked effective-permission set carried by the access token
/// (the <c>perm</c> claims). The 3-tier union (global/tenant/org) is resolved ONCE at
/// token-issue time by <see cref="EffectivePermissionResolver"/> and stamped into the token;
/// per-request enforcement trusts those claims as authoritative and does not re-query the store.
/// </summary>
public sealed class DefaultAuthorizationService : IFrameworkAuthorizationService
{
    private readonly ILogger<DefaultAuthorizationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAuthorizationService"/> class.
    /// </summary>
    public DefaultAuthorizationService(
        IServiceConfigurationProvider<RoleConfiguration> roleProvider,
        IServiceConfigurationProvider<PermissionConfiguration> permissionProvider,
        IServiceConfigurationProvider<RolePermissionConfiguration> rolePermissionProvider,
        ILogger<DefaultAuthorizationService>? logger,
        Lazy<ITenantContext>? tenantContext = null,
        Lazy<IOrgContext>? orgContext = null,
        Lazy<IOrgAccessProvider>? orgAccessProvider = null)
    {
        ArgumentNullException.ThrowIfNull(roleProvider);
        ArgumentNullException.ThrowIfNull(permissionProvider);
        ArgumentNullException.ThrowIfNull(rolePermissionProvider);
        _logger = logger ?? NullLogger<DefaultAuthorizationService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> Authorize(
        IAuthenticationContext context,
        string resource,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
            return GenericResult<bool>.Failure(AuthorizationLog.AuthorizationContextNull(_logger));

        if (string.IsNullOrWhiteSpace(resource))
            return GenericResult<bool>.Failure(AuthorizationLog.ResourceRequired(_logger));

        if (string.IsNullOrWhiteSpace(action))
            return GenericResult<bool>.Failure(AuthorizationLog.ActionRequired(_logger));

        if (!context.IsAuthenticated)
        {
            AuthorizationLog.AuthorizationDeniedNotAuthenticated(_logger, resource, action);
            return GenericResult<bool>.Success(false);
        }

        var userPermissions = await GetEffectivePermissions(context, cancellationToken).ConfigureAwait(false);

        if (userPermissions is null)
            return GenericResult<bool>.Success(false);

        var permissionName = $"{resource}:{action}";

        var hasPermission = userPermissions.Contains(permissionName, StringComparer.OrdinalIgnoreCase) ||
                           userPermissions.Contains($"{resource}:*", StringComparer.OrdinalIgnoreCase) ||
                           userPermissions.Contains("*:*", StringComparer.OrdinalIgnoreCase);

        if (hasPermission)
            AuthorizationLog.AuthorizationGranted(_logger, context.UserId, permissionName);
        else
            AuthorizationLog.AuthorizationDenied(_logger, context.UserId, permissionName);

        return GenericResult<bool>.Success(hasPermission);
    }

    /// <inheritdoc />
    public Task<IGenericResult<bool>> HasRole(
        IAuthenticationContext context,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
            return Task.FromResult<IGenericResult<bool>>(
                GenericResult<bool>.Failure(AuthorizationLog.AuthorizationContextNull(_logger)));

        if (string.IsNullOrWhiteSpace(role))
            return Task.FromResult<IGenericResult<bool>>(
                GenericResult<bool>.Failure(AuthorizationLog.RoleRequired(_logger)));

        if (!context.IsAuthenticated)
            return Task.FromResult<IGenericResult<bool>>(GenericResult<bool>.Success(false));

        var userRoles = GetUserRoles(context);
        return Task.FromResult<IGenericResult<bool>>(
            GenericResult<bool>.Success(userRoles.Contains(role, StringComparer.OrdinalIgnoreCase)));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> HasPermission(
        IAuthenticationContext context,
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
            return GenericResult<bool>.Failure(AuthorizationLog.AuthorizationContextNull(_logger));

        if (string.IsNullOrWhiteSpace(permission))
            return GenericResult<bool>.Failure(AuthorizationLog.PermissionRequired(_logger));

        if (!context.IsAuthenticated)
            return GenericResult<bool>.Success(false);

        var userPermissions = await GetEffectivePermissions(context, cancellationToken).ConfigureAwait(false);

        if (userPermissions is null)
            return GenericResult<bool>.Success(false);

        return GenericResult<bool>.Success(userPermissions.Contains(permission, StringComparer.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public Task<IGenericResult<IEnumerable<string>>> GetRoles(
        IAuthenticationContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
            return Task.FromResult<IGenericResult<IEnumerable<string>>>(
                GenericResult<IEnumerable<string>>.Failure(AuthorizationLog.AuthorizationContextNull(_logger)));

        if (!context.IsAuthenticated)
            return Task.FromResult<IGenericResult<IEnumerable<string>>>(
                GenericResult<IEnumerable<string>>.Success(Enumerable.Empty<string>()));

        var roles = GetUserRoles(context);
        return Task.FromResult<IGenericResult<IEnumerable<string>>>(
            GenericResult<IEnumerable<string>>.Success(roles));
    }

    private static HashSet<string> GetUserRoles(IAuthenticationContext context)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in context.Roles)
            roles.Add(role);

        return roles;
    }

    private Task<HashSet<string>?> GetEffectivePermissions(
        IAuthenticationContext context, CancellationToken cancellationToken)
    {
        var permissions = new HashSet<string>(context.Permissions, StringComparer.OrdinalIgnoreCase);

        AuthorizationLog.ThreeTierPermissionsResolved(
            _logger, 0, 0, 0, permissions.Count, context.UserId);

        return Task.FromResult<HashSet<string>?>(permissions);
    }
}
