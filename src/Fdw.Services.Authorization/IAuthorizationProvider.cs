using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;

namespace Fdw.Services.Authorization;

/// <summary>
/// Domain provider interface for authorization configuration.
/// Provides access to roles, permissions, and role-permission assignments
/// through the dual-source (ctrl + cfg) configuration model.
/// </summary>
// Why: Authorization had no proper provider interface. Consumers previously depended on
// IServiceConfigurationProvider<RoleConfiguration> (synchronous, no child loading) or
// raw IOptionsMonitor. This interface exposes the full authorization hierarchy with
// async child assembly (role → permissions).
public interface IAuthorizationProvider
{
    /// <summary>
    /// Gets a role configuration by name.
    /// </summary>
    Task<RoleConfiguration?> GetRole(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role configuration by ID.
    /// </summary>
    Task<RoleConfiguration?> GetRole(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all role configurations (system + user, deduplicated).
    /// </summary>
    Task<IReadOnlyList<RoleConfiguration>> GetAllRoles(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role configurations filtered by request context visibility rules.
    /// </summary>
    Task<IReadOnlyList<RoleConfiguration>> GetRoles(IRequestContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permission configurations.
    /// </summary>
    Task<IReadOnlyList<PermissionConfiguration>> GetPermissions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role-permission assignments for a specific role.
    /// </summary>
    Task<IReadOnlyList<RolePermissionConfiguration>> GetRolePermissions(Guid roleId, CancellationToken cancellationToken = default);
}
