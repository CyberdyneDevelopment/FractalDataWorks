using System;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>One permission that a role a person is assigned grants, with the assignment and the role it comes from (FDW-2688).</summary>
/// <param name="AssignmentTenantId">The tenant the person holds the role in.</param>
/// <param name="ExpiresAt">When the assignment ends, or none when it does not.</param>
/// <param name="RoleId">The role's id.</param>
/// <param name="RoleName">The role's name.</param>
/// <param name="IsTenantScoped">True when the role is one tenant's own definition, false when it is shared by every tenant.</param>
/// <param name="RoleTenantId">The tenant that owns the role.</param>
/// <param name="PermissionName">The permission the role grants, or none for a role that grants nothing.</param>
public sealed record UserPermissionGrant(
    Guid AssignmentTenantId,
    DateTimeOffset? ExpiresAt,
    Guid RoleId,
    string RoleName,
    bool IsTenantScoped,
    Guid RoleTenantId,
    string? PermissionName);
