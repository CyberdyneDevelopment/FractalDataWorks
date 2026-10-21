using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The one rule for which organization a caller may administer (FDW-1593): the organization they act in, in the tenant they act in.
/// </summary>
/// <remarks>
/// A person who administers Finance acts in Finance, so Legal is refused: the check is of the organization the request is for against
/// the one the caller's token acts in, and of the organization's tenant against the caller's. The system context passes, as it does
/// everywhere the platform acts for itself. Whether the caller holds the right to administer at all is the policy on the endpoint; this
/// is only where.
/// </remarks>
public interface IOrganizationScope
{
    /// <summary>Says whether the caller may administer an organization, and which tenant it belongs to.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization's tenant, or the reason the caller may not administer it or it does not exist.</returns>
    Task<IGenericResult<Guid>> TenantOf(Guid organizationId, CancellationToken cancellationToken = default);
}
