using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connections a tenant's administrator assigns to its organizations (FDW-1581).
/// </summary>
/// <remarks>
/// Each call is for one organization the caller may administer (<c>IOrganizationScope</c>). An organization has one assignment per
/// purpose, and it can use only a grant its own tenant holds: a connection the tenant was not granted cannot be assigned, and a base
/// folder cannot leave the grant's folder, because it is placed inside it.
/// </remarks>
public interface IConnectionAssignments
{
    /// <summary>Assigns an organization one of its tenant's grants for a purpose, replacing an earlier assignment for that purpose.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="purpose">The purpose, which must be the grant's.</param>
    /// <param name="grantId">The grant, which must be the organization's tenant's.</param>
    /// <param name="baseFolder">The folder inside the grant's the organization is narrowed to, or <see langword="null"/> for the grant as it is.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The assignment, or the reason it was not made.</returns>
    Task<IGenericResult<ConnectionAssignmentView>> Assign(Guid organizationId, string purpose, Guid grantId, string? baseFolder, CancellationToken cancellationToken = default);

    /// <summary>Takes the assignment of a purpose away from an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="purpose">The purpose.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The assignment that was removed, or the reason it was kept.</returns>
    Task<IGenericResult<ConnectionAssignmentView>> Unassign(Guid organizationId, string purpose, CancellationToken cancellationToken = default);

    /// <summary>Lists the assignments of an organization.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The assignments, or the reason they could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<ConnectionAssignmentView>>> List(Guid organizationId, CancellationToken cancellationToken = default);
}
