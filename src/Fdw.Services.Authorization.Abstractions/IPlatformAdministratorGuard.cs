using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authorization.Abstractions;

/// <summary>
/// Keeps at least one platform administrator: a person who holds the administrator role in the global tenant.
/// </summary>
/// <remarks>
/// Why a guard and not a seed rule: nobody holds a special account (Mike's decision), so the only protection against
/// locking the platform out is refusing the change that would remove the last holder. The grant rule asks who may give a
/// role; this asks whether taking it away leaves anyone able to give it back.
/// </remarks>
public interface IPlatformAdministratorGuard
{
    /// <summary>
    /// Says whether a person may lose a role in a tenant without leaving the platform with no administrator.
    /// </summary>
    /// <param name="userId">The person losing the role.</param>
    /// <param name="roleName">The role being taken away.</param>
    /// <param name="tenantId">The tenant the role is held in.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the change leaves a platform administrator; otherwise the reason.</returns>
    Task<IGenericResult> CanRemove(Guid userId, string roleName, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Says whether a role may be deleted without leaving the platform with no administrator.
    /// </summary>
    /// <param name="roleName">The role being deleted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the deletion leaves a platform administrator; otherwise the reason.</returns>
    Task<IGenericResult> CanDeleteRole(string roleName, CancellationToken cancellationToken = default);
}
