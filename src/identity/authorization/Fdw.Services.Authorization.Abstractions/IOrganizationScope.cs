using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The one rule for which organization a caller may administer (FDW-1593, FDW-2840, FDW-2855): one that exists, and for which they hold the
/// right at the organization's scope.
/// </summary>
/// <remarks>
/// The organization a caller acts in decides nothing here: acting is a context switch. A right held in the tenant reaches every organization
/// of it, and a right an organization grants reaches that organization only, so an administrator of Finance is refused Legal while an
/// administrator of the tenant is refused neither. Whether the caller may reach the organization's tenant at all is the scope authority's to
/// say, once: it refuses a tenant the caller does not act in and admits a platform administrator. The system context passes, as it does
/// everywhere the platform acts for itself.
/// </remarks>
public interface IOrganizationScope
{
    /// <summary>
    /// Says which tenant an organization belongs to. It decides nothing about tenants or rights: a caller of this overload asks the scope
    /// authority for the right it needs, which judges both.
    /// </summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization's tenant, or the reason the caller is not signed in or the organization does not exist.</returns>
    Task<IGenericResult<Guid>> TenantOf(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Says which tenant an organization belongs to when the caller holds a right at the organization's scope, tenant included.
    /// </summary>
    /// <param name="right">The right asked for, <c>{resource}:{action}</c>: see <see cref="OrganizationRights"/>.</param>
    /// <param name="organizationId">The organization.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The organization's tenant, or the reason the caller may not do this to it or it does not exist.</returns>
    Task<IGenericResult<Guid>> TenantOf(string right, Guid organizationId, CancellationToken cancellationToken = default);
}
