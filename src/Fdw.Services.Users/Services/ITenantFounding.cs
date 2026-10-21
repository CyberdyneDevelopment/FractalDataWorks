using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Users.Services;

/// <summary>
/// Makes a tenant together with its first administrator (FDW-1578): the tenant is never left with nobody who can enter it.
/// </summary>
/// <remarks>
/// Who may make a tenant at all is decided by the policy on the endpoint that calls this. This checks that the caller may give the
/// role, that the person exists, then makes the tenant, makes the person a member of it and gives them the role; if either of the last
/// two fails it removes what it made and reports the failure.
/// </remarks>
public interface ITenantFounding
{
    /// <summary>Makes a tenant and appoints its first administrator.</summary>
    /// <param name="tenant">What the tenant is.</param>
    /// <param name="administratorId">The person who becomes the tenant's first administrator; they must already have an account.</param>
    /// <param name="roleName">The role the person is given in the new tenant, which the caller must hold the rights of.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new tenant's id, or the reason nothing was made.</returns>
    Task<IGenericResult<Guid>> Found(NewTenant tenant, Guid administratorId, string roleName, CancellationToken cancellationToken = default);
}
