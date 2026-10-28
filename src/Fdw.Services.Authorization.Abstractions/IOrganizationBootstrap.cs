using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Gives a new organization its starting roles and first administrator (FDW-1758).
/// </summary>
/// <remarks>
/// <para>
/// The ordinary role and member services judge an organization by the caller's right at its scope, which a person making the organization
/// does not yet hold in it. This acts for the tenant instead: whoever calls it has already been shown to act in the tenant that owns the
/// organization.
/// The rule that a caller may give only what they hold still applies to every role copied and every role given.
/// </para>
/// </remarks>
public interface IOrganizationBootstrap
{
    /// <summary>Gives an organization a copy of the roles of another organization of the same tenant.</summary>
    /// <param name="tenantId">The tenant that owns both organizations.</param>
    /// <param name="fromOrganizationId">The organization whose roles are copied.</param>
    /// <param name="toOrganizationId">The organization that receives them.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The roles the organization now has, or the reason none were copied.</returns>
    Task<IGenericResult<IReadOnlyList<OrganizationRoleView>>> CopyRoles(
        Guid tenantId, Guid fromOrganizationId, Guid toOrganizationId, CancellationToken cancellationToken = default);

    /// <summary>Gives a person a role the organization has, making them a member of it.</summary>
    /// <param name="tenantId">The tenant that owns the organization.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="userId">The person.</param>
    /// <param name="roleName">The role.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The role the person now holds, or the reason it was not given.</returns>
    Task<IGenericResult<OrganizationMemberChange>> GiveRole(
        Guid tenantId, Guid organizationId, Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>Removes every role and every member an organization has, to undo a start that could not be finished.</summary>
    /// <param name="tenantId">The tenant that owns the organization.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success, or the reason something was left behind.</returns>
    Task<IGenericResult> Clear(Guid tenantId, Guid organizationId, CancellationToken cancellationToken = default);
}
