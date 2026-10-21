using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The people in an organization and the one role each holds there (FDW-1593).
/// </summary>
/// <remarks>
/// Each call is for one organization the caller may administer (<see cref="IOrganizationScope"/>). A person holds one role in an
/// organization; giving another replaces it. A caller can give only a role whose permissions they hold (<see cref="IGrantRule"/>). The
/// person must already have an account: the caller checks, because accounts are not this service's.
/// </remarks>
public interface IOrganizationMembers
{
    /// <summary>Gives a person a role in an organization, replacing the one they held there.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="userId">The person.</param>
    /// <param name="roleName">The role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What changed, or the reason nothing did.</returns>
    Task<IGenericResult<OrganizationMemberChange>> Give(Guid organizationId, Guid userId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>Takes a person out of an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="userId">The person.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The role they held there, or the reason they were not removed.</returns>
    Task<IGenericResult<string>> Remove(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lists the people in an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Each person and their role, or the reason they could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<OrganizationMember>>> List(Guid organizationId, CancellationToken cancellationToken = default);
}
