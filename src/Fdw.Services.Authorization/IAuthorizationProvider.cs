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
public interface IAuthorizationProvider
{
    /// <summary>
    /// Gets a role configuration by name.
    /// </summary>
    Task<RoleImplementationConfiguration?> GetRole(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role configuration by ID.
    /// </summary>
    Task<RoleImplementationConfiguration?> GetRole(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all role configurations (system + user, deduplicated).
    /// </summary>
    Task<IReadOnlyList<RoleImplementationConfiguration>> GetAllRoles(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role configurations filtered by request context visibility rules.
    /// </summary>
    Task<IReadOnlyList<RoleImplementationConfiguration>> GetRoles(IRequestContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permission configurations.
    /// </summary>
    Task<IReadOnlyList<PermissionImplementationConfiguration>> GetPermissions(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role-permission assignments for a specific role.
    /// </summary>
    Task<IReadOnlyList<RolePermissionImplementationConfiguration>> GetRolePermissions(Guid roleId, CancellationToken cancellationToken = default);
}
