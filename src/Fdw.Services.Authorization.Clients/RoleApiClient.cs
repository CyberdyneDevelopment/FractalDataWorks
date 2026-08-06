namespace Fdw.Services.Authorization.Clients;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for role and permission management.
/// </summary>
public class RoleApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleApiClient"/> class.
    /// </summary>
    public RoleApiClient(HttpClient httpClient, ILogger<RoleApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets a list of all roles.
    /// </summary>
    /// <returns>A result containing the list of role summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<RoleSummaryPayload>>> GetRoles(CancellationToken ct = default)
        => GetList<RoleSummaryPayload>("roles", ct);

    /// <summary>
    /// Gets a specific role by its name.
    /// </summary>
    /// <returns>A result containing the role detail.</returns>
    public virtual Task<IGenericResult<RoleDetailPayload>> GetRole(string name, CancellationToken ct = default)
        => Get<RoleDetailPayload>($"roles/{name}", ct);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <returns>A result containing the created role detail.</returns>
    public virtual Task<IGenericResult<RoleDetailPayload>> CreateRole(CreateRolePayload request, CancellationToken ct = default)
        => Post<CreateRolePayload, RoleDetailPayload>("roles", request, ct);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <returns>A result containing the updated role detail.</returns>
    public virtual Task<IGenericResult<RoleDetailPayload>> UpdateRole(string name, UpdateRolePayload request, CancellationToken ct = default)
        => Put<UpdateRolePayload, RoleDetailPayload>($"roles/{name}", request, ct);

    /// <summary>
    /// Deletes a specific role.
    /// </summary>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public virtual Task<IGenericResult> DeleteRole(string name, CancellationToken ct = default)
        => Delete($"roles/{name}", ct);

    /// <summary>
    /// Gets a list of all available permissions.
    /// </summary>
    /// <returns>A result containing the list of permissions.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<PermissionPayload>>> GetPermissions(CancellationToken ct = default)
        => GetList<PermissionPayload>("permissions", ct);

    /// <summary>
    /// Gets all permissions grouped by resource.
    /// </summary>
    /// <returns>A result containing the grouped permissions.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<PermissionGroupPayload>>> GetPermissionsGrouped(CancellationToken ct = default)
        => GetList<PermissionGroupPayload>("permissions/grouped", ct);

    /// <summary>
    /// Gets the permissions assigned to a specific role.
    /// </summary>
    /// <returns>A result containing the role's permissions.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<PermissionPayload>>> GetRolePermissions(string name, CancellationToken ct = default)
        => GetList<PermissionPayload>($"roles/{name}/permissions", ct);

    /// <summary>
    /// Sets the permissions for a specific role.
    /// </summary>
    /// <returns>A result indicating whether the permission update succeeded.</returns>
    public virtual Task<IGenericResult> SetRolePermissions(string name, SetRolePermissionsPayload request, CancellationToken ct = default)
        => Put($"roles/{name}/permissions", request, ct);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <returns>A result containing the user's updated roles.</returns>
    public virtual Task<IGenericResult<UserRolesPayload>> AssignUserRole(System.Guid userId, AssignRolePayload request, CancellationToken ct = default)
        => Post<AssignRolePayload, UserRolesPayload>($"users/{userId}/roles", request, ct);

    /// <summary>
    /// Revokes a role from a user.
    /// </summary>
    /// <returns>A result indicating whether the role revocation succeeded.</returns>
    public virtual Task<IGenericResult> RevokeUserRole(System.Guid userId, string roleName, CancellationToken ct = default)
        => Delete($"users/{userId}/roles/{roleName}", ct);

    /// <summary>
    /// Gets the roles assigned to a user.
    /// </summary>
    /// <returns>A result containing the user's roles.</returns>
    public virtual Task<IGenericResult<UserRolesPayload>> GetUserRoles(System.Guid userId, CancellationToken ct = default)
        => Get<UserRolesPayload>($"users/{userId}/roles", ct);
}
