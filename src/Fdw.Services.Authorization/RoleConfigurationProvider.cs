using Fdw.Results;
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
/// <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> with permission-aggregation helpers.
/// </summary>
public class RoleConfigurationProvider : ImplementationConfigurationProviderBase<RoleConfiguration, IRoleImplementationConfiguration, RoleConfigurationCommand>, IAuthorizationProvider, IRoleConfigurationProvider
{

    private readonly ILogger _logger;


    /// <summary>Initializes a new instance of the <see cref="RoleConfigurationProvider"/> class.</summary>
    public RoleConfigurationProvider(
        ILogger<RoleConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "authz")
        : base(logger ?? NullLogger<RoleConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<RoleConfigurationProvider>.Instance;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Overridden to drop superseded and soft-deleted versions. A role is deleted by the versioned
    /// write path -- the current row is stamped IsCurrent=0 and a copy is inserted with
    /// IsDeleted=1 -- so nothing is removed and an unfiltered read keeps returning it. Filtering
    /// here rather than in each caller is the FDW-732 lesson one table across: three callers went
    /// through that read and none of them filtered.
    /// </remarks>
    public override async Task<IGenericResult<IReadOnlyList<RoleConfiguration>>> Get(CancellationToken ct = default)
    {
        var all = await base.Get(ct).ConfigureAwait(false);
        if (!all.IsSuccess || all.Value is null) return all;

        return GenericResult<IReadOnlyList<RoleConfiguration>>.Success(
            [.. all.Value.Where(r => r.IsCurrent && !r.IsDeleted)]);
    }

    /// <inheritdoc />
    /// <remarks>A deleted role answers as absent, not as itself. See <see cref="Get(CancellationToken)"/>.</remarks>
    public override async Task<IGenericResult<RoleConfiguration>> Get(string name, CancellationToken ct = default)
    {
        var role = await base.Get(name, ct).ConfigureAwait(false);
        return Live(role);
    }

    /// <inheritdoc />
    /// <remarks>A deleted role answers as absent, not as itself. See <see cref="Get(CancellationToken)"/>.</remarks>
    public override async Task<IGenericResult<RoleConfiguration>> Get(Guid id, CancellationToken ct = default)
    {
        var role = await base.Get(id, ct).ConfigureAwait(false);
        return Live(role);
    }

    // Null rather than a failure: the caller asked whether a role by this name exists, and a
    // deleted one does not. Callers already treat a null value as not-found.
    private static IGenericResult<RoleConfiguration> Live(IGenericResult<RoleConfiguration> role)
        => role.IsSuccess && role.Value is { IsCurrent: true, IsDeleted: false }
            ? role
            : role.IsSuccess
                ? GenericResult<RoleConfiguration>.Success(null!)
                : role;

    /// <inheritdoc />
    public virtual async Task<RoleConfiguration?> GetRole(string name, CancellationToken cancellationToken = default)
    {
        var result = await Get(name, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value : null;
    }

    /// <inheritdoc />
    public virtual async Task<RoleConfiguration?> GetRole(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await Get(id, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value : null;
    }

    /// <inheritdoc />
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

        var result = await Execute<IEnumerable<PermissionConfiguration>>(command, cancellationToken).ConfigureAwait(false);
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
        var command = new QueryCommandBuilder<RolePermissionConfiguration>(
                DataStoreName, PathName, "RolePermission")
            .Where("RoleId", roleId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Build();

        var result = await Execute<IEnumerable<RolePermissionConfiguration>>(command, cancellationToken).ConfigureAwait(false);
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
