using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// The write side of tenants (FDW-1578), beside the reads of <see cref="ITenantProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// It writes the tenant and nothing around it. Who may make a tenant, who becomes its first administrator, and what must be gone
/// before a tenant is deleted are the caller's to settle first; the writer refuses only what would damage the tenants themselves: a
/// slug another tenant has, a change to a tenant that does not exist, and disabling or deleting the global tenant, which the platform
/// itself runs in.
/// </para>
/// </remarks>
public interface ITenantWriter
{
    /// <summary>Makes a tenant.</summary>
    /// <param name="tenant">What the tenant is.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new tenant's id, or the reason it was not made.</returns>
    Task<IGenericResult<Guid>> Create(NewTenant tenant, CancellationToken cancellationToken = default);

    /// <summary>Changes the parts of a tenant that the change states.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="change">What to change.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the reason the tenant was not changed.</returns>
    Task<IGenericResult> Update(Guid tenantId, TenantChange change, CancellationToken cancellationToken = default);

    /// <summary>Removes a tenant.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the reason the tenant was not removed.</returns>
    Task<IGenericResult> Delete(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Marks a tenant deleted, or clears the mark, keeping every row it holds (FDW-1759).</summary>
    /// <remarks>
    /// A deleted tenant leaves every list and cannot be signed in to, but nothing it holds is touched, so it can be brought back. Only a
    /// disabled tenant can be marked deleted, so a delete is never one step from a working tenant; the global tenant never can. Clearing
    /// the mark leaves the tenant disabled: enabling it again is a separate change.
    /// </remarks>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="isDeleted">True to mark it deleted, false to clear the mark.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or the reason the mark was not changed.</returns>
    Task<IGenericResult> SetDeleted(Guid tenantId, bool isDeleted, CancellationToken cancellationToken = default);
}
