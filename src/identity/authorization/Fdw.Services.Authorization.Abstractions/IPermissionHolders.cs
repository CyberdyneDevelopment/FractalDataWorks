using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Says who holds a permission (FDW-2144): the people to tell when something happens that needs it, such as every developer when a UI comment is saved.
/// </summary>
/// <remarks>
/// A person holds a permission in a tenant when a current, unexpired role assignment they have in that tenant carries it, the same rule
/// <see cref="IUserPermissions"/> applies when it bakes a token. <see cref="Get"/> and <see cref="GetOnPlatform"/> do not count the organization tier;
/// <see cref="GetInOrganization"/> counts only it. Assignments whose subject is not a person's Guid, such as a service identity's name, are not people and are
/// left out.
/// </remarks>
public interface IPermissionHolders
{
    /// <summary>
    /// Lists the people who hold a permission in a tenant.
    /// </summary>
    /// <param name="permissionName">The permission, such as <c>ui-feedback:read</c>. It must exist; asking about one that does not is a failure.</param>
    /// <param name="tenantId">The tenant whose assignments count.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The people, each once, or the failure naming what could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<Guid>>> Get(string permissionName, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the people who hold a permission in the global tenant: the platform administrators and whoever else the platform gave it to.
    /// </summary>
    /// <param name="permissionName">The permission. It must exist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The people, each once, or the failure naming what could not be read, including when exactly one tenant is not marked global.</returns>
    Task<IGenericResult<IReadOnlyList<Guid>>> GetOnPlatform(string permissionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the people who hold a permission in one organization of a tenant through what the organization grants them: the role they were given there,
    /// whose permissions are kept as the organization's access rows, the tier <see cref="IUserPermissions"/> adds when a token names the organization.
    /// </summary>
    /// <param name="permissionName">The permission. It must exist.</param>
    /// <param name="tenantId">The tenant the organization belongs to.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The people, each once, or the failure naming what could not be read.</returns>
    /// <remarks>A person who holds the permission only through a role assigned in the tenant is not listed here; <see cref="Get"/> lists them.</remarks>
    Task<IGenericResult<IReadOnlyList<Guid>>> GetInOrganization(string permissionName, Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);
}
