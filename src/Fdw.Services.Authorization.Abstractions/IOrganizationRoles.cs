using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The roles an organization defines for its own members (FDW-1593).
/// </summary>
/// <remarks>
/// Each call is for one organization the caller may administer (<see cref="IOrganizationScope"/>). A role carries at least one
/// permission, because the row-security filters admit a person to an organization by having any current access row for it, and a role
/// with no permission would write none. A caller can put into a role only the permissions they hold (<see cref="IGrantRule"/>). Changing
/// a role refreshes the access rows of everyone who holds it.
/// </remarks>
public interface IOrganizationRoles
{
    /// <summary>Defines a role in an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="roleName">The name, unique within the organization.</param>
    /// <param name="description">What the role is for, or <see langword="null"/> for none.</param>
    /// <param name="permissions">The permissions the role carries; at least one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The role as defined, or the reason it was not.</returns>
    Task<IGenericResult<OrganizationRoleView>> Define(Guid organizationId, string roleName, string? description, IReadOnlyList<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>Changes the description and permissions of a role, and refreshes the access of everyone who holds it.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="roleName">The role.</param>
    /// <param name="description">What the role is for, or <see langword="null"/> for none.</param>
    /// <param name="permissions">The permissions the role carries from now; at least one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The role as it is now, or the reason it was not changed.</returns>
    Task<IGenericResult<OrganizationRoleView>> Redefine(Guid organizationId, string roleName, string? description, IReadOnlyList<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>Deletes a role nobody holds and the organization was not made with.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="roleName">The role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the reason the role was kept.</returns>
    Task<IGenericResult> Delete(Guid organizationId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>Gets one role of an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="roleName">The role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The role, or the reason it could not be read.</returns>
    Task<IGenericResult<OrganizationRoleView>> Get(Guid organizationId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>Lists the roles of an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles by name, or the reason they could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<OrganizationRoleView>>> List(Guid organizationId, CancellationToken cancellationToken = default);
}
