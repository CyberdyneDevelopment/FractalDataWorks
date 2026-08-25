using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Authorization;

/// <summary>
/// Domain configuration provider for roles. Thin wrapper over
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/> with permission-aggregation helpers.
/// </summary>
public class RoleConfigurationProvider : DefaultConfigurationProvider<RoleConfiguration, RoleConfigurationCommand>, IAuthorizationProvider
{
    private readonly ILogger _logger;

    /// <summary>
    /// Registers this provider (and its interface forwards) as the domain's canonical singletons,
    /// targeting this domain's own default location. To override, call <c>SetConfiguration</c> on
    /// the resolved singleton.
    /// </summary>
    /// <remarks>
    /// Why: any option whose services depend on role configuration calls THIS cascade instead of
    /// re-registering the provider itself. A second registration site with a different lifetime or
    /// a bare ctor registration (DI filling the dataStoreName/pathName defaults) made the runtime
    /// lifetime depend on option discovery order and silently substituted default store names —
    /// both are the exact defect class this cascade pattern exists to prevent. Idempotent (TryAdd).
    /// This method is now parameterless for the same reason: a second registration call is never
    /// the legal way to change the location — <c>SetConfiguration</c> on the already-resolved
    /// singleton is.
    /// </remarks>
    public static IServiceCollection RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<RoleConfigurationProvider>(sp =>
            new RoleConfigurationProvider(
                sp.GetService<ILogger<RoleConfigurationProvider>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>()));
        services.TryAddSingleton<DefaultConfigurationProvider<RoleConfiguration, RoleConfigurationCommand>>(
            sp => sp.GetRequiredService<RoleConfigurationProvider>());
        services.TryAddSingleton<IAuthorizationProvider>(sp =>
            sp.GetRequiredService<RoleConfigurationProvider>());
        services.TryAddSingleton<IServiceConfigurationProvider<RoleConfiguration>>(sp =>
            sp.GetRequiredService<RoleConfigurationProvider>());
        return services;
    }

    /// <summary>Initializes a new instance of the <see cref="RoleConfigurationProvider"/> class.</summary>
    public RoleConfigurationProvider(
        ILogger<RoleConfigurationProvider>? logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "authz")
        : base(logger ?? NullLogger<RoleConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<RoleConfigurationProvider>.Instance;
    }

    /// <inheritdoc />
    // Why: virtual allows Moq to override GetRole in unit tests (e.g., DefaultPrincipalResolverTests)
    // without requiring a real IOptionsMonitor<List<RoleConfiguration>> or gateway.
    public virtual async Task<RoleConfiguration?> GetRole(string name, CancellationToken cancellationToken = default)
    {
        var result = await Get(name, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value : null;
    }

    /// <inheritdoc />
    // Why: virtual — same test-isolation rationale as GetRole(string).
    public virtual async Task<RoleConfiguration?> GetRole(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Get(id, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value : null;
    }

    /// <inheritdoc />
    // Why: virtual to match GetByUser — both are the seams the auth resolver tests stub on a
    // loose mock. Without virtual, a loose mock runs the real body against a null gateway and NREs.
    public virtual async Task<IReadOnlyList<RoleConfiguration>> GetAllRoles(CancellationToken cancellationToken = default)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            RoleConfigurationProviderLog.RolesQueryFailed(_logger);
            return [];
        }
        return result.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleConfiguration>> GetRoles(
        IRequestContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await Get(cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            RoleConfigurationProviderLog.FilteredRolesQueryFailed(_logger);
            return [];
        }
        return result.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionConfiguration>> GetPermissions(
        CancellationToken cancellationToken = default)
    {
        var command = new QueryCommandBuilder<PermissionConfiguration>(
                DataStoreName, PathName, "Permission")
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .OrderBy("Domain")
            .Build();

        var result = await Gateway.Execute<IEnumerable<PermissionConfiguration>>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            RoleConfigurationProviderLog.PermissionQueryFailed(_logger, result.CurrentMessage ?? "Unknown error");
            return [];
        }

        var permissions = result.Value.ToList();
        RoleConfigurationProviderLog.AllPermissionsLoaded(_logger, permissions.Count);
        return permissions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RolePermissionConfiguration>> GetRolePermissions(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        // Why: version-on-write tables keep all historical rows. Filter to the currently-active,
        // non-deleted slice so duplicate or soft-deleted assignments don't inflate the result.
        var command = new QueryCommandBuilder<RolePermissionConfiguration>(
                DataStoreName, PathName, "RolePermission")
            .Where("RoleId", roleId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await Gateway.Execute<IEnumerable<RolePermissionConfiguration>>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
        {
            RoleConfigurationProviderLog.RolePermissionQueryFailed(_logger, roleId.ToString(), result.CurrentMessage ?? "Unknown error");
            return [];
        }

        var rolePermissions = result.Value.ToList();
        RoleConfigurationProviderLog.RolePermissionsLoaded(_logger, rolePermissions.Count, roleId.ToString());
        return rolePermissions;
    }

    /// <summary>Gets a role with its permissions assembled onto it.</summary>
    public async Task<RoleConfiguration?> GetWithPermissions(
        string name,
        CancellationToken cancellationToken = default)
    {
        var result = await Get(name, cancellationToken).ConfigureAwait(false);
        // Why: split query-failed from found-but-absent (FDW-583) — the prior single check fired one
        // Trace log for both "no such role" and "ConfigurationDb query failed", silently vanishing
        // role permissions on an infrastructure fault. IsSuccess=false is the query failure (Error);
        // IsSuccess=true with a null Value is a genuine miss (Debug).
        if (!result.IsSuccess)
        {
            RoleConfigurationProviderLog.RoleQueryFailed(_logger, name, result.CurrentMessage ?? "Unknown error");
            return null;
        }
        if (result.Value is null)
        {
            RoleConfigurationProviderLog.RoleNotFound(_logger, name);
            return null;
        }

        var role = result.Value;
        RoleConfigurationProviderLog.LoadingPermissions(_logger, role.Name, role.Id.ToString());

        var rolePermissions = await GetRolePermissions(role.Id, cancellationToken).ConfigureAwait(false);

        RoleConfigurationProviderLog.PermissionsAssembled(_logger, rolePermissions.Count, role.Name);

        return role;
    }
}
