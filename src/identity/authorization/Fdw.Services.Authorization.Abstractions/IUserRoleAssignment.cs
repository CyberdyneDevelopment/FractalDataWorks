using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// The one place a role is given to a person in a tenant (FDW-1578): the assign endpoint and the creation of a tenant, which gives its
/// first administrator the Admin role in the new tenant, both call it.
/// </summary>
/// <remarks>
/// It writes the grant and nothing else. Whether the caller may give the role is <see cref="IGrantRule"/>, and whether the change may
/// happen at all is <see cref="IPlatformAdministratorGuard"/>; both are asked before this is called. A role the person already holds in
/// the tenant is left as it is.
/// </remarks>
public interface IUserRoleAssignment
{
    /// <summary>Gives a role to a person in a tenant.</summary>
    /// <param name="userId">The person.</param>
    /// <param name="roleName">The name of the role, which the service resolves.</param>
    /// <param name="tenantId">The tenant the role is held in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the person holds the role in the tenant afterwards; otherwise the reason the role was not found or could not be read or written.</returns>
    Task<IGenericResult> Assign(Guid userId, string roleName, Guid tenantId, CancellationToken cancellationToken = default);
}
