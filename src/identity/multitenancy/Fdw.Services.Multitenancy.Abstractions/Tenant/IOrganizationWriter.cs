using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The write side of organizations (FDW-1758), beside the reads of <see cref="IOrganizationProvider"/>.
/// </summary>
/// <remarks>
/// It writes the organization and nothing around it. Who may make one, who becomes its first administrator and which roles it starts with
/// are the caller's to settle first; the writer refuses only what would damage the organizations themselves: a slug another organization of
/// the tenant has, a change to an organization that does not exist, and disabling or removing the default organization of a tenant, which
/// the tenant's people act in until they choose another.
/// </remarks>
public interface IOrganizationWriter
{
    /// <summary>Makes an organization.</summary>
    /// <param name="organization">What the organization is.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new organization's id, or the reason it was not made.</returns>
    Task<IGenericResult<Guid>> Create(NewOrganization organization, CancellationToken cancellationToken = default);

    /// <summary>Changes the parts of an organization that the change states.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="change">What to change.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the reason the organization was not changed.</returns>
    Task<IGenericResult> Update(Guid organizationId, OrganizationChange change, CancellationToken cancellationToken = default);

    /// <summary>Removes an organization that was just made and could not be finished.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the reason the organization was not removed.</returns>
    Task<IGenericResult> Delete(Guid organizationId, CancellationToken cancellationToken = default);
}
